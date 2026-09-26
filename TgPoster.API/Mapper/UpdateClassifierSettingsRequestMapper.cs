using TgPoster.API.Domain.UseCases.Discover.UpdateClassifierSettings;
using TgPoster.API.Models;

namespace TgPoster.API.Mapper;

internal static class UpdateClassifierSettingsRequestMapper
{
	public static UpdateClassifierSettingsCommand ToDomain(this UpdateClassifierSettingsRequest request) =>
		new(
			request.IsEnabled,
			request.Model,
			request.BatchSize,
			request.IntervalMinutes,
			request.MessageSampleCount,
			request.PhotoCount,
			request.ReclassifyAfterDays,
			request.Categories,
			request.SystemPrompt,
			request.TelegramSessionId);
}
