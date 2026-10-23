namespace TgPoster.API.Domain.UseCases.Messages.RetryMessages;

/// <summary>
/// Данные поста для проверки и расчёта повторной отправки
/// </summary>
/// <param name="Id"></param>
/// <param name="ScheduleId"></param>
/// <param name="IsFailed"></param>
/// <param name="TimePosting"></param>
public sealed record RetryMessageInfo(Guid Id, Guid ScheduleId, bool IsFailed, DateTimeOffset TimePosting);
