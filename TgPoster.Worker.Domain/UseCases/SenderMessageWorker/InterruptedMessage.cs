namespace TgPoster.Worker.Domain.UseCases.SenderMessageWorker;

/// <summary>
/// Пост, отправка которого прервалась перезапуском воркера и которй помечен ошибкой
/// </summary>
/// <param name="MessageId"></param>
/// <param name="ScheduleId"></param>
public sealed record InterruptedMessage(Guid MessageId, Guid ScheduleId);
