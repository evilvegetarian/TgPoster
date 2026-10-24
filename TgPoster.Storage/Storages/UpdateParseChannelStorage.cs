using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Parse.UpdateParseChannel;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Storages;

internal sealed class UpdateParseChannelStorage(PosterContext context) : IUpdateParseChannelStorage
{
	public Task<bool> ExistParseChannelAsync(Guid id, Guid userId, CancellationToken cancellationToken)
	{
		return context.ChannelParsingParameters.AnyAsync(x => x.Id == id && x.Schedule.UserId == userId,
			cancellationToken);
	}

	public Task<bool> ExistScheduleAsync(Guid scheduleId, Guid userId, CancellationToken cancellationToken)
	{
		return context.Schedules.AnyAsync(x => x.Id == scheduleId && x.UserId == userId, cancellationToken);
	}

	public Task<bool> ExistTelegramSessionAsync(
		Guid telegramSessionId,
		Guid userId,
		CancellationToken cancellationToken
	)
	{
		return context.TelegramSessions.AnyAsync(x => x.Id == telegramSessionId && x.UserId == userId,
			cancellationToken);
	}

	public async Task UpdateParseChannelAsync(UpdateParseChannelCommand command, CancellationToken cancellationToken)
	{
		var entity = await context.ChannelParsingParameters.FirstAsync(x => x.Id == command.Id, cancellationToken);

		// Позиция парсинга относится к конкретному каналу: правка остальных настроек её не сбрасывает,
		// иначе следующий запуск заново спарсит весь канал и наплодит дубли
		if (!IsSameChannel(entity.Channel, command.Channel))
		{
			entity.LastParseId = null;
			entity.LastParseDate = null;
			entity.TotalMessagesCount = null;
			entity.Status = ParsingStatus.New;
		}

		entity.Channel = command.Channel;
		entity.ScheduleId = command.ScheduleId;
		entity.AvoidWords = command.AvoidWords;
		entity.DateFrom = command.DateFrom;
		entity.DateTo = command.DateTo;
		entity.DeleteText = command.DeleteText;
		entity.DeleteMedia = command.DeleteMedia;
		entity.CheckNewPosts = command.AlwaysCheckNewPosts;
		entity.NeedVerifiedPosts = command.NeedVerifiedPosts;
		entity.UseAiForPosts = command.UseAiForPosts;
		entity.TelegramSessionId = command.TelegramSessionId;
		entity.SkipAds = command.SkipAds;

		await context.SaveChangesAsync(cancellationToken);
	}

	private static bool IsSameChannel(string current, string updated) =>
		string.Equals(NormalizeChannel(current), NormalizeChannel(updated), StringComparison.OrdinalIgnoreCase);

	private static string NormalizeChannel(string channel)
	{
		var name = channel.Trim();
		foreach (var prefix in new[] { "https://t.me/", "http://t.me/", "t.me/" })
		{
			if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
			{
				name = name[prefix.Length..];
			}
		}

		return name.TrimStart('@').TrimEnd('/');
	}
}