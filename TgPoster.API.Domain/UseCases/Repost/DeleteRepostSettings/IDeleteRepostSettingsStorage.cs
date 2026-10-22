namespace TgPoster.API.Domain.UseCases.Repost.DeleteRepostSettings;

public interface IDeleteRepostSettingsStorage
{
	/// <summary>
	/// Проверяет, что настройки репоста существуют и принадлежат пользователю
	/// </summary>
	/// <param name="id"></param>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<bool> RepostSettingsExistsAsync(Guid id, Guid userId, CancellationToken ct);

	/// <summary>
	///     Удаляет настройки репоста и все связанные destinations.
	/// </summary>
	Task DeleteRepostSettingsAsync(Guid id, CancellationToken ct);
}