using System.Text.RegularExpressions;
using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Telegram;
using TgPoster.Worker.Domain.ConfigModels;
using TL;

namespace TgPoster.Worker.Domain.UseCases.DiscoverChannelLinks;

internal sealed partial class DiscoverChannelLinksWorker(
	IDiscoverChannelLinksStorage storage,
	ITelegramAuthService authService,
	TelegramOptions telegramOptions,
	ILogger<DiscoverChannelLinksWorker> logger,
	IHostApplicationLifetime lifetime)
{
	private const int MessageBatchSize = 100;

	[DisableConcurrentExecution(100000)]
	public async Task ProcessChannelsAsync()
	{
		var ct = lifetime.ApplicationStopping;

		var channels = await storage.GetChannelsToProcessAsync(ct);
		channels = channels.Where(x => x.LastParsedId == null).ToList();
		if (channels.Count == 0)
		{
			logger.LogDebug("Нет каналов для обработки DiscoverChannelLinks");
			return;
		}

		logger.LogInformation("Начинаем обработку {Count} каналов для поиска ссылок", channels.Count);

		var client = await authService.GetClientAsync(telegramOptions.TelegramSessionId, ct);

		foreach (var channelDto in channels)
		{
			try
			{
				await ProcessChannelAsync(client, channelDto, ct);
				await Task.Delay(TimeSpan.FromMinutes(1), ct);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Ошибка при обработке канала {Channel}",
					channelDto.Username ?? channelDto.TelegramId?.ToString());
			}
		}
	}

	private async Task ProcessChannelAsync(
		WTelegram.Client client,
		DiscoverChannelDto channelDto,
		CancellationToken ct
	)
	{
		Channel? channel;

		if (!string.IsNullOrEmpty(channelDto.Username))
		{
			logger.LogInformation("Поиск TG-ссылок в канале @{Channel}", channelDto.Username);
			var resolveResult = await client.Contacts_ResolveUsername(channelDto.Username);
			channel = resolveResult.Chat as Channel;
		}
		else if (channelDto.TelegramId.HasValue)
		{
			logger.LogInformation("Поиск TG-ссылок в приватном канале ID={TelegramId}", channelDto.TelegramId);
			var dialogs = await client.Messages_GetAllDialogs();
			channel = dialogs.chats.TryGetValue(channelDto.TelegramId.Value, out var chatBase)
				? chatBase as Channel
				: null;
		}
		else
		{
			logger.LogDebug("Пропускаем канал только с инвайт-хешем — нет доступа для сканирования");
			return;
		}

		if (channel is null)
		{
			logger.LogWarning("Не удалось найти канал: {Channel}",
				channelDto.Username ?? channelDto.TelegramId?.ToString());
			return;
		}

		var telegramId = channel.ID;
		var sourcePeerType = ResolvePeerType(channel);
		var allUsernames = new Dictionary<string, DiscoveredPeerInfo>(StringComparer.OrdinalIgnoreCase);
		var privateChats = new Dictionary<long, DiscoveredPeerInfo>();
		var lastParsedId = channelDto.LastParsedId ?? 0;
		var offset = 0;
		var textUsernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var inviteHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var privateChannelIds = new HashSet<long>();

		while (true)
		{
			ct.ThrowIfCancellationRequested();

			var history = await client.Messages_GetHistory(
				new InputPeerChannel(channel.ID, channel.access_hash),
				limit: MessageBatchSize,
				offset_id: offset,
				min_id: channelDto.LastParsedId ?? 0);

			if (history.Messages.Length == 0)
				break;

			var chats = new Dictionary<long, ChatBase>();
			var users = new Dictionary<long, User>();
			history.CollectUsersChats(users, chats);

			foreach (var keyValuePair in chats)
			{
				var chat = keyValuePair.Value;
				if (chat is ChatForbidden or ChannelForbidden)
				{
					logger.LogDebug("Чат {Id} \"{Title}\" заблокирован, пропускаем", chat.ID, chat.Title);
					continue;
				}

				if (!string.IsNullOrEmpty(chat.MainUsername))
				{
					allUsernames.TryAdd(chat.MainUsername, new DiscoveredPeerInfo
					{
						PeerType = ResolvePeerType(chat),
						Username = chat.MainUsername,
						TelegramId = keyValuePair.Key,
						Title = chat.Title,
						ParticipantsCount = (chat as Channel)?.participants_count
					});
				}
				else if (keyValuePair.Key != 0)
				{
					privateChats.TryAdd(keyValuePair.Key, new DiscoveredPeerInfo
					{
						PeerType = ResolvePeerType(chat),
						Username = null,
						TelegramId = keyValuePair.Key,
						Title = chat.Title,
						ParticipantsCount = (chat as Channel)?.participants_count
					});
				}
			}

			foreach (var message in history.Messages.OfType<Message>())
			{
				if (lastParsedId < message.ID)
					lastParsedId = message.ID;

				if (string.IsNullOrEmpty(message.message))
					continue;
				ExtractLinksFromText(message.message, textUsernames, inviteHashes, privateChannelIds);
			}

			offset = history.Messages.Last().ID;

			if (history.Messages.Length < MessageBatchSize)
				break;
		}

		// Добавляем приватные каналы из t.me/c/ID ссылок
		foreach (var privId in privateChannelIds)
		{
			privateChats.TryAdd(privId, new DiscoveredPeerInfo
			{
				PeerType = "channel",
				Username = null,
				TelegramId = privId,
				Title = null,
				ParticipantsCount = null
			});
		}

		// Проверяем текстовые юзернеймы — оставляем только каналы/группы/чаты и сохраняем тип peer
		var resolvedTextPeers = await ResolveChatPeersAsync(client, textUsernames, ct);
		foreach (var resolvedPeer in resolvedTextPeers.Values)
		{
			allUsernames.TryAdd(resolvedPeer.Username!, resolvedPeer);
		}

		// Резолвим инвайт-ссылки — получаем метаданные без вступления
		var resolvedInvites = await ResolveInviteLinksAsync(client, inviteHashes, ct);

		if (channelDto.Username is not null)
			allUsernames.Remove(channelDto.Username);

		logger.LogInformation(
			"Найдено {PublicCount} публичных, {PrivateCount} приватных, {InviteCount} по инвайтам в {Channel}",
			allUsernames.Count, privateChats.Count, resolvedInvites.Count,
			channelDto.Username ?? channelDto.TelegramId?.ToString());

		// Сохраняем публичные каналы
		foreach (var peer in allUsernames.Values)
		{
			await storage.UpsertAsync(
				peer.Username,
				$"https://t.me/{peer.Username}",
				lastParsedId: null,
				telegramId: peer.TelegramId,
				peerType: peer.PeerType,
				title: peer.Title,
				participantsCount: peer.ParticipantsCount,
				ct: ct);
		}

		// Сохраняем приватные каналы из CollectUsersChats и t.me/c/ID
		foreach (var peer in privateChats.Values)
		{
			await storage.UpsertAsync(
				username: null,
				tgUrl: null,
				lastParsedId: null,
				telegramId: peer.TelegramId,
				peerType: peer.PeerType,
				title: peer.Title,
				participantsCount: peer.ParticipantsCount,
				ct: ct);
		}

		// Сохраняем каналы из инвайт-ссылок
		foreach (var (hash, peer) in resolvedInvites)
		{
			await storage.UpsertAsync(
				username: peer.Username,
				tgUrl: $"https://t.me/+{hash}",
				lastParsedId: null,
				telegramId: peer.TelegramId != 0 ? peer.TelegramId : null,
				peerType: peer.PeerType,
				title: peer.Title,
				participantsCount: peer.ParticipantsCount,
				inviteHash: hash,
				ct: ct);
		}

		// Сохраняем последнее спарсенное сообщение, TelegramId и тип peer исходного канала
		if (lastParsedId > 0)
		{
			await storage.UpsertAsync(
				channelDto.Username,
				tgUrl: null,
				lastParsedId: lastParsedId,
				telegramId: telegramId,
				peerType: sourcePeerType,
				title: channel.title,
				participantsCount: channel.participants_count,
				markAsCompleted: true,
				ct: ct);
		}
	}

	private async Task<Dictionary<string, DiscoveredPeerInfo>> ResolveChatPeersAsync(
		WTelegram.Client client,
		HashSet<string> usernames,
		CancellationToken ct
	)
	{
		var chats = new Dictionary<string, DiscoveredPeerInfo>(StringComparer.OrdinalIgnoreCase);

		foreach (var username in usernames)
		{
			ct.ThrowIfCancellationRequested();

			try
			{
				var result = await client.Contacts_ResolveUsername(username);
				if (result.Chat is null)
				{
					logger.LogDebug("@{Username} — пользователь, пропускаем", username);
				}
				else
				{
					chats[username] = new DiscoveredPeerInfo
					{
						PeerType = ResolvePeerType(result.Chat),
						TelegramId = result.Chat.ID,
						Username = username,
						Title = result.Chat.Title,
						ParticipantsCount = (result.Chat as Channel)?.participants_count
					};
				}
			}
			catch (Exception ex)
			{
				logger.LogDebug(ex, "Не удалось разрешить @{Username}, пропускаем", username);
			}

			await Task.Delay(500, ct);
		}

		return chats;
	}

	private async Task<Dictionary<string, DiscoveredPeerInfo>> ResolveInviteLinksAsync(
		WTelegram.Client client,
		HashSet<string> hashes,
		CancellationToken ct
	)
	{
		var results = new Dictionary<string, DiscoveredPeerInfo>(StringComparer.OrdinalIgnoreCase);

		foreach (var hash in hashes)
		{
			ct.ThrowIfCancellationRequested();

			try
			{
				var inviteInfo = await client.Messages_CheckChatInvite(hash);

				switch (inviteInfo)
				{
					case ChatInviteAlready { chat: var chat }:
						results[hash] = new DiscoveredPeerInfo
						{
							Username = chat.MainUsername,
							TelegramId = chat.ID,
							PeerType = ResolvePeerType(chat),
							Title = chat.Title,
							ParticipantsCount = (chat as Channel)?.participants_count,
							InviteHash = hash
						};
						break;
					case ChatInvite invite:
						results[hash] = new DiscoveredPeerInfo
						{
							Username = null,
							TelegramId = 0,
							PeerType = invite.flags.HasFlag(ChatInvite.Flags.channel) ? "channel" : "chat",
							Title = invite.title,
							ParticipantsCount = invite.participants_count,
							InviteHash = hash
						};
						break;
				}
			}
			catch (Exception ex)
			{
				logger.LogDebug(ex, "Не удалось проверить инвайт-хеш {Hash}, пропускаем", hash);
			}

			await Task.Delay(1000, ct);
		}

		return results;
	}

	private static string ResolvePeerType(Channel channel)
	{
		if (channel.IsGroup)
			return "chat";

		if (channel.IsChannel)
			return "channel";

		return "chat";
	}

	private static string ResolvePeerType(ChatBase channel)
	{
		if (channel.IsGroup)
			return "chat";

		if (channel.IsChannel)
			return "channel";

		return "chat";
	}

	private static void ExtractLinksFromText(
		string text,
		HashSet<string> usernames,
		HashSet<string> inviteHashes,
		HashSet<long> privateChannelIds
	)
	{
		// t.me/username ссылки (исключая joinchat/, +, c/)
		foreach (Match match in TmeLinkRegex().Matches(text))
		{
			var username = match.Groups[1].Value;
			if (username.Length >= 5)
				usernames.Add(username);
		}

		// @username упоминания
		foreach (Match match in MentionRegex().Matches(text))
		{
			var username = match.Groups[1].Value;
			if (username.Length >= 5)
				usernames.Add(username);
		}

		// t.me/+HASH или t.me/joinchat/HASH — инвайт-ссылки
		foreach (Match match in InviteLinkRegex().Matches(text))
		{
			inviteHashes.Add(match.Groups[1].Value);
		}

		// t.me/c/ID — приватные каналы по ID
		foreach (Match match in PrivateChannelLinkRegex().Matches(text))
		{
			if (long.TryParse(match.Groups[1].Value, out var id))
				privateChannelIds.Add(id);
		}
	}

	[GeneratedRegex(@"(?:https?://)?t\.me/(?!(?:joinchat/|\+|c/))([a-zA-Z0-9_]{5,})", RegexOptions.Compiled)]
	private static partial Regex TmeLinkRegex();

	[GeneratedRegex(@"@([a-zA-Z0-9_]{5,})", RegexOptions.Compiled)]
	private static partial Regex MentionRegex();

	[GeneratedRegex(@"(?:https?://)?t\.me/(?:joinchat/|\+)([\w\-]+)", RegexOptions.Compiled)]
	private static partial Regex InviteLinkRegex();

	[GeneratedRegex(@"(?:https?://)?t\.me/c/(\d+)(?:/\d+)?", RegexOptions.Compiled)]
	private static partial Regex PrivateChannelLinkRegex();

	private readonly record struct DiscoveredPeerInfo(
		string? Username,
		long TelegramId,
		string PeerType,
		string? Title = null,
		int? ParticipantsCount = null,
		string? InviteHash = null);
}