using TgPoster.API.Domain.UseCases.Discover.UpdateDiscoverSettings;
using TgPoster.API.Models;

namespace TgPoster.API.Mapper;

internal static class UpdateDiscoverSettingsRequestMapper
{
	public static UpdateDiscoverSettingsCommand ToDomain(this UpdateDiscoverSettingsRequest request) =>
		new(
			request.IsEnabled,
			request.IntervalMinutes,
			request.ChannelsPerSession,
			request.TelegramSessionIds);
}
