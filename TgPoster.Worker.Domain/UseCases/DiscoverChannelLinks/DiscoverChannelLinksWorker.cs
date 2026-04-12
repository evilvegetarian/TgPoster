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

	[DisableConcurrentExecution(96 * 60 * 60)]
	public async Task ProcessChannelsAsync()
	{
		var ct = lifetime.ApplicationStopping;

		var channels = await storage.GetChannelsToProcessAsync(ct);
		if (channels.Count == 0)
		{
			logger.LogInformation("Нет каналов для обработки DiscoverChannelLinks");
			return;
		}

		logger.LogInformation("Начинаем обработку {Count} каналов для поиска ссылок", channels.Count);

		var client = await authService.GetClientAsync(telegramOptions.TelegramSessionId, ct);

		foreach (var channelDto in channels)
		{
			try
			{
				await ProcessChannelAsync(client, channelDto, ct);
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
		var channel = await ResolveChannelAsync(client, channelDto);
		if (channel is null)
		{
			logger.LogWarning("Не удалось найти канал: {Channel}",
				channelDto.Username ?? channelDto.TelegramId?.ToString());
			return;
		}

		var scan = await ScanChannelHistoryAsync(client, channel, channelDto, ct);

		foreach (var privId in scan.PrivateChannelIds)
		{
			scan.PrivatePeers.TryAdd(privId, new DiscoveredPeerInfo
			{
				PeerType = "channel",
				TelegramId = privId
			});
		}

		var resolvedTextPeers = await ResolveChatPeersAsync(client, scan.TextUsernames, ct);
		foreach (var resolvedPeer in resolvedTextPeers.Values)
			scan.PublicPeers.TryAdd(resolvedPeer.Username!, resolvedPeer);

		var resolvedInvites = await ResolveInviteLinksAsync(client, scan.InviteHashes, ct);

		DeduplicateResolvedInvites(scan.PublicPeers, scan.PrivatePeers, resolvedInvites);
		DeduplicateByTitle(scan.PublicPeers, scan.PrivatePeers, resolvedInvites);

		if (channelDto.Username is not null)
			scan.PublicPeers.Remove(channelDto.Username);

		logger.LogInformation(
			"Найдено {PublicCount} публичных, {PrivateCount} приватных, {InviteCount} по инвайтам в {Channel}",
			scan.PublicPeers.Count, scan.PrivatePeers.Count, resolvedInvites.Count,
			channelDto.Username ?? channelDto.TelegramId?.ToString());

		await SaveDiscoveredPeersAsync(channelDto, channel, scan, resolvedInvites, ct);
	}

	private async Task<Channel?> ResolveChannelAsync(WTelegram.Client client, DiscoverChannelDto channelDto)
	{
		if (!string.IsNullOrEmpty(channelDto.Username))
		{
			logger.LogInformation("Поиск TG-ссылок в канале @{Channel}", channelDto.Username);
			var resolveResult = await client.Contacts_ResolveUsername(channelDto.Username);
			return resolveResult.Chat as Channel;
		}

		if (channelDto.TelegramId.HasValue)
		{
			logger.LogInformation("Поиск TG-ссылок в приватном канале ID={TelegramId}", channelDto.TelegramId);
			var dialogs = await client.Messages_GetAllDialogs();
			return dialogs.chats.TryGetValue(channelDto.TelegramId.Value, out var chatBase)
				? chatBase as Channel
				: null;
		}

		logger.LogDebug("Пропускаем канал только с инвайт-хешем — нет доступа для сканирования");
		return null;
	}

	private async Task<HistoryScanResult> ScanChannelHistoryAsync(
		WTelegram.Client client,
		Channel channel,
		DiscoverChannelDto channelDto,
		CancellationToken ct
	)
	{
		var publicPeers = new Dictionary<string, DiscoveredPeerInfo>(StringComparer.OrdinalIgnoreCase);
		var privatePeers = new Dictionary<long, DiscoveredPeerInfo>();
		var textUsernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var inviteHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var privateChannelIds = new HashSet<long>();
		var lastParsedId = channelDto.LastParsedId ?? 0;
		var offset = 0;

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
					continue;

				if (!string.IsNullOrEmpty(chat.MainUsername))
				{
					publicPeers.TryAdd(chat.MainUsername, new DiscoveredPeerInfo
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
					privatePeers.TryAdd(keyValuePair.Key, new DiscoveredPeerInfo
					{
						PeerType = ResolvePeerType(chat),
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

				if (!string.IsNullOrEmpty(message.message))
					ExtractLinksFromText(message.message, textUsernames, inviteHashes, privateChannelIds);

				ExtractLinksFromEntities(message.entities, textUsernames, inviteHashes, privateChannelIds);
			}

			offset = history.Messages.Last().ID;

			if (history.Messages.Length < MessageBatchSize)
				break;

			await Task.Delay(TimeSpan.FromSeconds(10), ct);
		}

		return new HistoryScanResult(publicPeers, privatePeers, textUsernames, inviteHashes, privateChannelIds,
			lastParsedId);
	}

	private async Task SaveDiscoveredPeersAsync(
		DiscoverChannelDto channelDto,
		Channel channel,
		HistoryScanResult scan,
		Dictionary<string, DiscoveredPeerInfo> resolvedInvites,
		CancellationToken ct
	)
	{
		foreach (var peer in scan.PublicPeers.Values)
		{
			await storage.UpsertAsync(new DiscoveredPeerUpsert
			{
				Username = peer.Username,
				TgUrl = $"https://t.me/{peer.Username}",
				TelegramId = peer.TelegramId,
				PeerType = peer.PeerType,
				Title = peer.Title,
				ParticipantsCount = peer.ParticipantsCount,
				InviteHash = peer.InviteHash,
				DiscoveredFromChannelId = channelDto.Id
			}, ct);
		}

		foreach (var peer in scan.PrivatePeers.Values)
		{
			await storage.UpsertAsync(new DiscoveredPeerUpsert
			{
				TelegramId = peer.TelegramId,
				PeerType = peer.PeerType,
				Title = peer.Title,
				ParticipantsCount = peer.ParticipantsCount,
				InviteHash = peer.InviteHash,
				DiscoveredFromChannelId = channelDto.Id
			}, ct);
		}

		foreach (var (hash, peer) in resolvedInvites)
		{
			await storage.UpsertAsync(new DiscoveredPeerUpsert
			{
				Username = peer.Username,
				TgUrl = $"https://t.me/+{hash}",
				TelegramId = peer.TelegramId != 0 ? peer.TelegramId : null,
				PeerType = peer.PeerType,
				Title = peer.Title,
				ParticipantsCount = peer.ParticipantsCount,
				InviteHash = hash,
				DiscoveredFromChannelId = channelDto.Id
			}, ct);
		}

		await storage.UpsertAsync(new DiscoveredPeerUpsert
		{
			Username = channelDto.Username,
			LastParsedId = scan.LastParsedId,
			TelegramId = channel.ID,
			PeerType = ResolvePeerType(channel),
			Title = channel.title,
			ParticipantsCount = channel.participants_count,
			MarkAsCompleted = true
		}, ct);
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

			await Task.Delay(TimeSpan.FromSeconds(3), ct);
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
					case ChatInvitePeek { chat: var chat }:
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
							TelegramId = 0,
							PeerType = ResolvePeerType(invite),
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

			await Task.Delay(TimeSpan.FromSeconds(3), ct);
		}

		return results;
	}

	private static void DeduplicateResolvedInvites(
		Dictionary<string, DiscoveredPeerInfo> allUsernames,
		Dictionary<long, DiscoveredPeerInfo> privateChats,
		Dictionary<string, DiscoveredPeerInfo> resolvedInvites
	)
	{
		var hashesToRemove = new List<string>();
		var seenTelegramIds = new HashSet<long>();

		foreach (var (hash, peer) in resolvedInvites)
		{
			if (peer.Username is not null && allUsernames.TryGetValue(peer.Username, out var existingByUsername))
			{
				if (existingByUsername.InviteHash is null)
					allUsernames[peer.Username] = existingByUsername with { InviteHash = hash };

				hashesToRemove.Add(hash);
				continue;
			}

			if (peer.TelegramId != 0 && privateChats.TryGetValue(peer.TelegramId, out var existingPrivate))
			{
				if (peer.Username is not null)
				{
					allUsernames.TryAdd(peer.Username, peer);
					privateChats.Remove(peer.TelegramId);
				}
				else if (existingPrivate.InviteHash is null)
				{
					privateChats[peer.TelegramId] = existingPrivate with { InviteHash = hash };
				}

				hashesToRemove.Add(hash);
				continue;
			}

			if (peer.TelegramId != 0 && !seenTelegramIds.Add(peer.TelegramId))
			{
				hashesToRemove.Add(hash);
			}
		}

		foreach (var hash in hashesToRemove)
			resolvedInvites.Remove(hash);
	}

	private static void DeduplicateByTitle(
		Dictionary<string, DiscoveredPeerInfo> allUsernames,
		Dictionary<long, DiscoveredPeerInfo> privateChats,
		Dictionary<string, DiscoveredPeerInfo> resolvedInvites
	)
	{
		var seenTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var peer in allUsernames.Values)
		{
			if (peer.Title is not null)
				seenTitles.Add(peer.Title);
		}

		var idsToRemove = new List<long>();
		foreach (var (id, peer) in privateChats)
		{
			if (peer.Title is not null && !seenTitles.Add(peer.Title))
				idsToRemove.Add(id);
		}

		foreach (var id in idsToRemove)
			privateChats.Remove(id);

		var hashesToRemove = new List<string>();
		foreach (var (hash, peer) in resolvedInvites)
		{
			if (peer.Username is null && peer.Title is not null && !seenTitles.Add(peer.Title))
				hashesToRemove.Add(hash);
		}

		foreach (var hash in hashesToRemove)
			resolvedInvites.Remove(hash);
	}

	private static string ResolvePeerType(ChatBase chat) => chat.IsChannel ? "channel" : "chat";

	private static string ResolvePeerType(ChatInvite invite) =>
		invite.flags.HasFlag(ChatInvite.Flags.channel) ? "channel" : "chat";

	private static void ExtractLinksFromEntities(
		MessageEntity[]? entities,
		HashSet<string> usernames,
		HashSet<string> inviteHashes,
		HashSet<long> privateChannelIds
	)
	{
		if (entities is null)
			return;

		foreach (var entity in entities)
		{
			if (entity is not MessageEntityTextUrl textUrl)
				continue;

			ExtractLinksFromText(textUrl.url, usernames, inviteHashes, privateChannelIds);
		}
	}

	private static void ExtractLinksFromText(
		string text,
		HashSet<string> usernames,
		HashSet<string> inviteHashes,
		HashSet<long> privateChannelIds
	)
	{
		foreach (Match match in TmeLinkRegex().Matches(text))
		{
			var username = match.Groups[1].Value;
			if (username.Length >= 5)
				usernames.Add(username);
		}

		foreach (Match match in MentionRegex().Matches(text))
		{
			var username = match.Groups[1].Value;
			if (username.Length >= 5)
				usernames.Add(username);
		}

		foreach (Match match in InviteLinkRegex().Matches(text))
			inviteHashes.Add(match.Groups[1].Value);

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

	private sealed record HistoryScanResult(
		Dictionary<string, DiscoveredPeerInfo> PublicPeers,
		Dictionary<long, DiscoveredPeerInfo> PrivatePeers,
		HashSet<string> TextUsernames,
		HashSet<string> InviteHashes,
		HashSet<long> PrivateChannelIds,
		int LastParsedId);

	private readonly record struct DiscoveredPeerInfo(
		string? Username = null,
		long TelegramId = 0,
		string PeerType = "channel",
		string? Title = null,
		int? ParticipantsCount = null,
		string? InviteHash = null);
}