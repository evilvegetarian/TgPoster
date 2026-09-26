namespace TgPoster.API.Domain.UseCases.SocialAccounts.ListSocialAccounts;

/// <summary>
///     Хранилище для получения списка аккаунтов соцсетей
/// </summary>
public interface IListSocialAccountsStorage
{
	/// <summary>
	///     Получить аккаунты соцсетей пользователя
	/// </summary>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<SocialAccountResponse>> GetAsync(Guid userId, CancellationToken ct);
}
