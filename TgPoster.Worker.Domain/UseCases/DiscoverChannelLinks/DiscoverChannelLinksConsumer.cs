using System.Text.RegularExpressions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Telegram;
using TL;

namespace TgPoster.Worker.Domain.UseCases.DiscoverChannelLinks;

internal sealed partial class DiscoverChannelLinksConsumer(
	IDiscoverChannelLinksStorage storage,
	ITelegramAuthService authService,
	IPublishEndpoint publishEndpoint,
	ILogger<DiscoverChannelLinksConsumer> logger) : IConsumer<DiscoverChannelLinksContract>
{
	private const int MaxDepth = 1;
	private const int MessageBatchSize = 100;

	public async Task Consume(ConsumeContext<DiscoverChannelLinksContract> context)
	{
		var msg = context.Message;
		var ct = context.CancellationToken;

		logger.LogInformation(
			"Поиск TG-ссылок в канале @{Channel}, глубина: {Depth}",
			msg.ChannelUsername, msg.Depth);

		var client = await authService.GetClientAsync(msg.TelegramSessionId, ct);

		var resolveResult = await client.Contacts_ResolveUsername(msg.ChannelUsername);
		if (resolveResult.Chat is not Channel channel)
		{
			logger.LogWarning("Не удалось найти канал: @{Channel}", msg.ChannelUsername);
			return;
		}

		var discoveredUsernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var lastParsedId = 0;
		var offset = 0;

		while (true)
		{
			ct.ThrowIfCancellationRequested();

			var history = await client.Messages_GetHistory(
				new InputPeerChannel(channel.ID, channel.access_hash),
				limit: MessageBatchSize,
				offset_id: offset);

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
					discoveredUsernames.Add(fwdChannel.username);
				}

				if (string.IsNullOrEmpty(message.message))
					continue;

				ExtractUsernames(message.message, discoveredUsernames);
			}

			offset = history.Messages.Last().ID;

			if (history.Messages.Length < MessageBatchSize)
				break;
		}

		// Убираем самого себя
		discoveredUsernames.Remove(msg.ChannelUsername);

		// Фильтруем ботов
		discoveredUsernames.RemoveWhere(u => u.EndsWith("_bot", StringComparison.OrdinalIgnoreCase)
		                                    || u.EndsWith("bot", StringComparison.OrdinalIgnoreCase));

		logger.LogInformation(
			"Найдено {Count} уникальных каналов в @{Channel}",
			discoveredUsernames.Count, msg.ChannelUsername);

		var newUsernames = new List<string>();

		foreach (var username in discoveredUsernames)
		{
			var exists = await storage.ExistsAsync(username, ct);
			if (exists)
				continue;

			await storage.UpsertAsync(username, $"@{msg.ChannelUsername}", lastParsedId: null, ct);
			newUsernames.Add(username);
		}

		// Рекурсивный запуск для найденных каналов (если не достигли максимальной глубины)
		if (msg.Depth < MaxDepth)
		{
			foreach (var username in newUsernames)
			{
				await publishEndpoint.Publish(new DiscoverChannelLinksContract
				{
					ChannelUsername = username,
					TelegramSessionId = msg.TelegramSessionId,
					Depth = msg.Depth + 1
				}, ct);
			}

			logger.LogInformation(
				"Запущен рекурсивный поиск для {Count} каналов (глубина {Depth} -> {NextDepth})",
				newUsernames.Count, msg.Depth, msg.Depth + 1);
		}
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
