namespace TgPoster.Worker.Domain.UseCases.SendCommentConsumer;

public interface ISendCommentConsumerStorage
{
	/// <summary>
	///     Создаёт запись в журнале комментирующего репоста.
	///     Если error != null, статус устанавливается как Failed, иначе Success.
	/// </summary>
	Task CreateLogAsync(
		Guid settingsId,
		int originalPostId,
		int? forwardedMessageId,
		int? commentMessageId,
		string? error,
		CancellationToken ct
	);

	/// <summary>
	/// Зафиксирован ли бан аккаунта сессии в чате
	/// </summary>
	/// <param name="telegramSessionId"></param>
	/// <param name="chatId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<bool> IsAccountBannedAsync(Guid telegramSessionId, long chatId, CancellationToken ct);

	/// <summary>
	/// Фиксирует бан аккаунта в группе обсуждения и выключает все настройки комментариев этой сессии,
	/// которые пишут в эту группу
	/// </summary>
	/// <param name="telegramSessionId"></param>
	/// <param name="discussionGroupId"></param>
	/// <param name="error"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task MarkAccountBannedAsync(Guid telegramSessionId, long discussionGroupId, string? error, CancellationToken ct);

	/// <summary>
	/// В группе обсуждения писать нельзя никому из рядовых участников: записывает запрет в Discover
	/// как свойство чата и выключает эти настройки комментариев
	/// </summary>
	/// <param name="settingsId"></param>
	/// <param name="discussionGroupId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task MarkChatWriteForbiddenAsync(Guid settingsId, long discussionGroupId, CancellationToken ct);
}