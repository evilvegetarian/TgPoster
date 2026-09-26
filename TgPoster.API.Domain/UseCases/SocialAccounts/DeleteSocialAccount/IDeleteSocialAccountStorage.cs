namespace TgPoster.API.Domain.UseCases.SocialAccounts.DeleteSocialAccount;

/// <summary>
///     Хранилище для удаления аккаунта соцсети
/// </summary>
public interface IDeleteSocialAccountStorage
{
	/// <summary>
	///     Проверить существование аккаунта у пользователя
	/// </summary>
	/// <param name="id"></param>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<bool> ExistsAsync(Guid id, Guid userId, CancellationToken ct);

	/// <summary>
	///     Удалить аккаунт соцсети и его связки
	/// </summary>
	/// <param name="id"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task DeleteAsync(Guid id, CancellationToken ct);
}
