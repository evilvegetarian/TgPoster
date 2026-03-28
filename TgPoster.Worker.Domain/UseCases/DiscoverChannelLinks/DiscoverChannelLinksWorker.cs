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
		channels = channels.Take(100).Skip(1).ToList();
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
				await Task.Delay(TimeSpan.FromMinutes(10), ct);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Ошибка при обработке канала @{Channel}", channelDto.Username);
			}
		}
	}

	private async Task ProcessChannelAsync(
		WTelegram.Client client,
		DiscoverChannelDto channelDto,
		CancellationToken ct
	)
	{
		logger.LogInformation("Поиск TG-ссылок в канале @{Channel}", channelDto.Username);

		var resolveResult = await client.Contacts_ResolveUsername(channelDto.Username);
		if (resolveResult.Chat is not Channel channel)
		{
			logger.LogWarning("Не удалось найти канал: @{Channel}", channelDto.Username);
			return;
		}

		var telegramId = channel.ID;
		var sourcePeerType = ResolvePeerType(channel);
		var allUsernames = new Dictionary<string, DiscoveredPeerInfo>(StringComparer.OrdinalIgnoreCase);
		var lastParsedId = channelDto.LastParsedId ?? 0;
		var offset = 0;
		var textUsernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
				if (string.IsNullOrEmpty(chat.MainUsername))
					continue;

				allUsernames.TryAdd(chat.MainUsername, new DiscoveredPeerInfo
				{
					PeerType = ResolvePeerType(chat),
					Username = chat.MainUsername,
					TelegramId = keyValuePair.Key,
					Title = chat.Title,
					ParticipantsCount = (chat as Channel)?.participants_count
				});
			}

			foreach (var message in history.Messages.OfType<Message>())
			{
				if (lastParsedId < message.ID)
					lastParsedId = message.ID;

				if (string.IsNullOrEmpty(message.message))
					continue;
				ExtractUsernames(message.message, textUsernames);
			}

			offset = history.Messages.Last().ID;

			if (history.Messages.Length < MessageBatchSize)
				break;
		}


		// Проверяем текстовые юзернеймы — оставляем только каналы/группы/чаты и сохраняем тип peer
		var resolvedTextPeers = await ResolveChatPeersAsync(client, textUsernames, ct);
		foreach (var resolvedPeer in resolvedTextPeers.Values)
		{
			allUsernames.TryAdd(resolvedPeer.Username, resolvedPeer);
		}

		// Фильтруем ботов
		foreach (var botUsername in allUsernames.Keys
			         .Where(u => u.EndsWith("_bot", StringComparison.OrdinalIgnoreCase)
			                     || u.EndsWith("bot", StringComparison.OrdinalIgnoreCase))
			         .ToList())
		{
			allUsernames.Remove(botUsername);
		}

		allUsernames.Remove(channelDto.Username);
		logger.LogInformation("Найдено {Count} уникальных каналов в @{Channel}", allUsernames.Count, channelDto.Username);

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
				ct);
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

	private static void ExtractUsernames(string text, HashSet<string> usernames)
	{
		// t.me/username ссылки
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
	}

	[GeneratedRegex(@"(?:https?://)?t\.me/([a-zA-Z0-9_]{5,})", RegexOptions.Compiled)]
	private static partial Regex TmeLinkRegex();

	[GeneratedRegex(@"@([a-zA-Z0-9_]{5,})", RegexOptions.Compiled)]
	private static partial Regex MentionRegex();

	private readonly record struct DiscoveredPeerInfo(
		string Username,
		long TelegramId,
		string PeerType,
		string? Title = null,
		int? ParticipantsCount = null);
}