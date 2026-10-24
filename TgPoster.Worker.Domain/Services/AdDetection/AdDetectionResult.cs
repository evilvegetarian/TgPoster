namespace TgPoster.Worker.Domain.Services.AdDetection;

/// <summary>
/// Результат проверки сообщения на рекламу: сработавшее правило или null, если рекламы нет
/// </summary>
/// <param name="IsAd"></param>
/// <param name="Reason"></param>
internal sealed record AdDetectionResult(bool IsAd, string? Reason);
