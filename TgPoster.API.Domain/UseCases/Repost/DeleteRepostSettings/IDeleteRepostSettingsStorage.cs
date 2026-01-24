namespace TgPoster.API.Domain.UseCases.Repost.DeleteRepostSettings;

public interface IDeleteRepostSettingsStorage
{
	/// <summary>
	///     Проверяет существуют ли настройки репоста с указанным ID.
	/// </summary>
	Task<bool> RepostSettingsExistsAsync(Guid id, CancellationToken ct);

	/// <summary>
	///     Удаляет настройки репоста и все связанные destinations.
	/// </summary>
	Task DeleteRepostSettingsAsync(Guid id, CancellationToken ct);
}
