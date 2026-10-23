using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data;
using TgPoster.Worker.Domain.Services.ScheduleAlerts;

namespace TgPoster.Storage.Storages;

internal sealed class ScheduleAlertStorage(PosterContext context) : IScheduleAlertStorage
{
	public Task<ScheduleAlertTarget?> GetAlertTargetAsync(Guid scheduleId, CancellationToken ct)
	{
		return context.Schedules
			.Where(s => s.Id == scheduleId)
			.Select(s => new ScheduleAlertTarget(
				s.TelegramBot.ApiTelegram,
				s.TelegramBot.ChatId,
				s.Name,
				s.ChannelName))
			.FirstOrDefaultAsync(ct);
	}
}
