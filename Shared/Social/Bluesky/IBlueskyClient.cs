namespace Shared.Social.Bluesky;

/// <summary>
///     Клиент Bluesky (AT Protocol) для создания сессии, загрузки blob и публикации записей
/// </summary>
public interface IBlueskyClient
{
	/// <summary>
	///     Создать сессию по идентификатору и app password
	/// </summary>
	/// <param name="identifier"></param>
	/// <param name="appPassword"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<BlueskyResult<BlueskySession>> CreateSessionAsync(string identifier, string appPassword, CancellationToken ct);

	/// <summary>
	///     Загрузить бинарный blob в репозиторий пользователя
	/// </summary>
	/// <param name="session"></param>
	/// <param name="data"></param>
	/// <param name="mimeType"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<BlueskyResult<BlueskyBlob>> UploadBlobAsync(BlueskySession session, byte[] data, string mimeType, CancellationToken ct);

	/// <summary>
	///     Создать пост в ленте Bluesky
	/// </summary>
	/// <param name="session"></param>
	/// <param name="record"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<BlueskyResult<BlueskyRecordRef>> CreatePostAsync(BlueskySession session, BlueskyPostRecord record, CancellationToken ct);
}
