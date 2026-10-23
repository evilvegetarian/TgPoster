namespace TgPoster.API.Domain.UseCases.Messages.ApproveMessages;

/// <summary>
/// Данные поста для подтверждения и переноса просроченого времени публикации
/// </summary>
/// <param name="Id"></param>
/// <param name="ScheduleId"></param>
/// <param name="IsVerified"></param>
/// <param name="IsRegistered"></param>
/// <param name="TimePosting"></param>
public sealed record ApproveMessageInfo(
	Guid Id,
	Guid ScheduleId,
	bool IsVerified,
	bool IsRegistered,
	DateTimeOffset TimePosting
);
