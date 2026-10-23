using MediatR;
using Security.IdentityServices;
using TgPoster.Telegram.Models;

namespace TgPoster.API.Domain.UseCases.Dashboard.GetDashboard;

/// <summary>
/// Use case данных главной страницы
/// </summary>
internal sealed class GetDashboardUseCase(
	IGetDashboardStorage storage,
	IIdentityProvider identity,
	TimeProvider timeProvider
) : IRequestHandler<GetDashboardQuery, DashboardResponse>
{
	private const int UpcomingLimit = 10;
	private const int FailedPostsLimit = 20;
	private const int RepostTopErrorsLimit = 3;
	private const string PhoneMask = "•••";

	public async Task<DashboardResponse> Handle(GetDashboardQuery request, CancellationToken ct)
	{
		var userId = identity.Current.UserId;
		var now = timeProvider.GetUtcNow();
		var since = now.AddHours(-24);

		// Хранилище делит один DbContext, поэтому запросы идут строго последовательно
		var schedules = await storage.GetSchedulesAsync(userId, now, ct);
		var upcoming = await storage.GetUpcomingPostsAsync(userId, now, UpcomingLimit, ct);
		var failedPosts = await storage.GetFailedPostsAsync(userId, since, FailedPostsLimit, ct);
		var failedTotal = await storage.GetFailedPostsCountAsync(userId, since, ct);
		var repostCounts = await storage.GetRepostCountsAsync(userId, since, ct);
		var repostErrors = await storage.GetRepostTopErrorsAsync(userId, since, RepostTopErrorsLimit, ct);
		var sessions = await storage.GetSessionsAsync(userId, ct);
		var parsingProblems = await storage.GetParsingProblemsAsync(userId, ct);
		var workers = await storage.GetWorkersAsync(ct);

		return new DashboardResponse
		{
			Schedules = schedules.ConvertAll(x => new DashboardSchedule(
				x.Id,
				x.Name,
				x.ChannelName,
				x.IsActive,
				x.HasSlots,
				x.PlannedCount,
				x.AwaitingApprovalCount,
				x.NextPostAt,
				x.LastPlannedAt,
				GetQueueDays(x.LastPlannedAt, now),
				x.FailedLast24h)),
			UpcomingPosts = upcoming,
			FailedPosts = failedPosts,
			FailedPostsTotal = failedTotal,
			Reposts = new DashboardReposts(
				repostCounts.SuccessCount,
				repostCounts.FailedCount,
				repostCounts.SkippedCount,
				repostErrors),
			TelegramSessions = sessions.ConvertAll(x => new DashboardSession(
				x.Id,
				x.Name,
				MaskPhone(x.PhoneNumber),
				x.Status,
				x.IsActive,
				x.FloodWaitUntil,
				!x.IsActive || x.Status != TelegramSessionStatus.Authorized || x.FloodWaitUntil > now)),
			ParsingProblems = parsingProblems,
			Workers = workers
		};
	}

	private static double? GetQueueDays(DateTimeOffset? lastPlannedAt, DateTimeOffset now)
	{
		if (lastPlannedAt is null)
		{
			return null;
		}

		return Math.Round(Math.Max(0, (lastPlannedAt.Value - now).TotalDays), 1);
	}

	private static string MaskPhone(string phoneNumber)
	{
		var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

		// Номер из 4 цифр и короче раскрылся бы целиком
		return digits.Length > 4 ? PhoneMask + digits[^4..] : PhoneMask;
	}
}
