using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.SocialAccounts.ConnectBluesky;

/// <summary>
///     Хранилище для подключения аккаунта Bluesky
/// </summary>
public interface IConnectBlueskyStorage
{
	/// <summary>
	///     Создать или обновить аккаунт соцсети
	/// </summary>
	/// <param name="userId"></param>
	/// <param name="platform"></param>
	/// <param name="name"></param>
	/// <param name="externalUserId"></param>
	/// <param name="encryptedSecret"></param>
	/// <param name="tokenExpiresAt"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<Guid> UpsertAsync(
		Guid userId,
		SocialPlatform platform,
		string name,
		string externalUserId,
		string encryptedSecret,
		DateTimeOffset? tokenExpiresAt,
		CancellationToken ct);
}
