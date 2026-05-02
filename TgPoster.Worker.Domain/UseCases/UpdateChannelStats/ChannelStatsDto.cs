namespace TgPoster.Worker.Domain.UseCases.UpdateChannelStats;

public sealed record ChannelStatsDto(Guid Id, string Username, long? TelegramId);
