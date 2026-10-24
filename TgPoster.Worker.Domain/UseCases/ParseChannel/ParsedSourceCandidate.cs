namespace TgPoster.Worker.Domain.UseCases.ParseChannel;

/// <summary>
/// Пост-кандидат из канала-источника: id его сообщения (для альбома — первого) и текст
/// для сверки со старыми постами, у которых id источника не сохранён
/// </summary>
/// <param name="SourceMessageId"></param>
/// <param name="Text"></param>
public sealed record ParsedSourceCandidate(int SourceMessageId, string? Text);
