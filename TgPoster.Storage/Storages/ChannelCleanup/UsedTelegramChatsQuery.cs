using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data;
using TgPoster.Telegram.Models;

namespace TgPoster.Storage.Storages.ChannelCleanup;

/// <summary>
/// Общая выборка чатов, которые приложение использует через сессию. Предпросмотр, запуск и воркер
/// обязаны считать одинаково, иначе воркер выйдет из канала, который предпросмотр оставил
/// </summary>
internal static class UsedTelegramChatsQuery
{
	public static async Task<UsedTelegramChats> LoadAsync(PosterContext context, Guid sessionId, CancellationToken ct)
	{
		// Сессию могли удалить, пока задание ждало: владельца всё равно нужно знать
		var userId = await context.TelegramSessions
			.IgnoreQueryFilters()
			.Where(x => x.Id == sessionId)
			.Select(x => x.UserId)
			.FirstAsync(ct);

		var destinationChatIds = await context.RepostDestinations
			.Where(x => x.RepostSettings.TelegramSessionId == sessionId)
			.Select(x => x.ChatId)
			.ToListAsync(ct);

		var repostSourceChatIds = await context.RepostSettings
			.Where(x => x.TelegramSessionId == sessionId)
			.Select(x => x.Schedule.ChannelId)
			.ToListAsync(ct);

		// Каналы расписаний — собственные каналы владельца, их не трогаем при любой сессии
		var scheduleChatIds = await context.Schedules
			.Where(x => x.UserId == userId)
			.Select(x => x.ChannelId)
			.ToListAsync(ct);

		var commentReposts = await context.CommentRepostSettings
			.Where(x => x.TelegramSessionId == sessionId)
			.Select(x => new { x.WatchedChannelId, x.DiscussionGroupId, x.WatchedChannel })
			.ToListAsync(ct);

		var parseChannels = await context.ChannelParsingParameters
			.Where(x => x.TelegramSessionId == sessionId)
			.Select(x => x.Channel)
			.ToListAsync(ct);

		return new UsedTelegramChats(
			[
				..destinationChatIds,
				..repostSourceChatIds,
				..scheduleChatIds,
				..commentReposts.Select(x => x.WatchedChannelId),
				..commentReposts.Select(x => x.DiscussionGroupId)
			],
			[..parseChannels, ..commentReposts.Select(x => x.WatchedChannel)]);
	}
}
