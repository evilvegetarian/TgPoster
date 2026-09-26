namespace TgPoster.API.Domain.UseCases.SocialAccounts.ConnectBluesky;

/// <summary>
///     Ответ после подключения аккаунта соцсети
/// </summary>
public sealed record ConnectSocialAccountResponse
{
	public required Guid Id { get; init; }
}
