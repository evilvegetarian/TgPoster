namespace TgPoster.Worker.Domain.UseCases.SenderMessageWorker;

/// <summary>
/// Id сообщений опубликованого поста в канале
/// </summary>
/// <param name="FirstId"></param>
/// <param name="AllIds"></param>
/// <param name="TextMessageId"></param>
internal sealed record SentMessageIds(int FirstId, int[] AllIds, int TextMessageId);
