using MediatR;

namespace TgPoster.API.Domain.UseCases.Discover.UpdateDiscoverSettings;

/// <summary>
/// Сохранить настройки поиска каналов (Discover)
/// </summary>
/// <param name="IsEnabled"></param>
/// <param name="IntervalMinutes"></param>
/// <param name="ChannelsPerSession"></param>
/// <param name="TelegramSessionIds"></param>
public sealed record UpdateDiscoverSettingsCommand(
	bool IsEnabled,
	int IntervalMinutes,
	int ChannelsPerSession,
	IReadOnlyList<Guid> TelegramSessionIds) : IRequest;
