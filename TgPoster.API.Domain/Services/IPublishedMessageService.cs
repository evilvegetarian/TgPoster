namespace TgPoster.API.Domain.Services;

/// <summary>
/// Изменения уже опубликованного в канале поста через бота расписания
/// </summary>
public interface IPublishedMessageService
{
	/// <summary>
	/// Заменить текст сообщения в канале: подпись к медиа или текст сообщения
	/// </summary>
	/// <param name="token"></param>
	/// <param name="channelId"></param>
	/// <param name="messageId"></param>
	/// <param name="isCaption"></param>
	/// <param name="html"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<ApiResult> EditTextAsync(
		string token,
		long channelId,
		int messageId,
		bool isCaption,
		string html,
		CancellationToken ct
	);

	/// <summary>
	/// Удалить сообщения поста из канала
	/// </summary>
	/// <param name="token"></param>
	/// <param name="channelId"></param>
	/// <param name="ids"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<ApiResult> DeleteAsync(string token, long channelId, int[] ids, CancellationToken ct);
}
