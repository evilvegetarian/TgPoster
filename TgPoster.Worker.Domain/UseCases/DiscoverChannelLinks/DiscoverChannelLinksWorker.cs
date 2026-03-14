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

	[DisableConcurrentExecution(120)]
	public async Task ProcessChannelsAsync()
	{
		var ct = lifetime.ApplicationStopping;

		var channels = await storage.GetChannelsToProcessAsync(ct);
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
		CancellationToken ct)
	{
		logger.LogInformation("Поиск TG-ссылок в канале @{Channel}", channelDto.Username);

		var resolveResult = await client.Contacts_ResolveUsername(channelDto.Username);
		if (resolveResult.Chat is not Channel channel)
		{
			logger.LogWarning("Не удалось найти канал: @{Channel}", channelDto.Username);
			return;
		}

		var telegramId = channel.ID;
		var forwardedUsernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var textUsernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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

			foreach (var message in history.Messages.OfType<Message>())
			{
				if (lastParsedId < message.ID)
					lastParsedId = message.ID;

				if (message.fwd_from?.from_id is PeerChannel fwdPeer
				    && chats.GetValueOrDefault(fwdPeer.channel_id) is Channel fwdChannel
				    && !string.IsNullOrEmpty(fwdChannel.username)
				    && fwdChannel.username.Length >= 5)
				{
					forwardedUsernames.Add(fwdChannel.username);
				}

				if (string.IsNullOrEmpty(message.message))
					continue;

				ExtractUsernames(message.message, textUsernames);
			}

			offset = history.Messages.Last().ID;

			if (history.Messages.Length < MessageBatchSize)
				break;
		}

		// Не резолвим юзернеймы, уже найденные через пересылки
		textUsernames.ExceptWith(forwardedUsernames);

		// Проверяем текстовые юзернеймы — оставляем только каналы/группы/чаты
		var verifiedTextUsernames = await FilterToChatsAsync(client, textUsernames, ct);

		var discoveredUsernames = new HashSet<string>(forwardedUsernames, StringComparer.OrdinalIgnoreCase);
		discoveredUsernames.UnionWith(verifiedTextUsernames);

		// Убираем самого себя
		discoveredUsernames.Remove(channelDto.Username);

		// Фильтруем ботов
		discoveredUsernames.RemoveWhere(u => u.EndsWith("_bot", StringComparison.OrdinalIgnoreCase)
		                                    || u.EndsWith("bot", StringComparison.OrdinalIgnoreCase));

		logger.LogInformation(
			"Найдено {Count} уникальных каналов в @{Channel}",
			discoveredUsernames.Count, channelDto.Username);

		foreach (var username in discoveredUsernames)
		{
			var exists = await storage.ExistsAsync(username, ct);
			if (exists)
				continue;

			await storage.UpsertAsync(username, $"@{channelDto.Username}", lastParsedId: null, telegramId: null, ct);
		}

		// Сохраняем последнее спарсенное сообщение и TelegramId
		if (lastParsedId > 0)
		{
			await storage.UpdateLastParsedIdAsync(channelDto.Username, lastParsedId, telegramId, ct);
		}
	}

	private async Task<HashSet<string>> FilterToChatsAsync(
		WTelegram.Client client,
		HashSet<string> usernames,
		CancellationToken ct)
	{
		var chats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var username in usernames)
		{
			ct.ThrowIfCancellationRequested();

			try
			{
				var result = await client.Contacts_ResolveUsername(username);
				if (result.Chat is not null)
				{
					chats.Add(username);
				}
				else
				{
					logger.LogDebug("@{Username} — пользователь, пропускаем", username);
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
}
