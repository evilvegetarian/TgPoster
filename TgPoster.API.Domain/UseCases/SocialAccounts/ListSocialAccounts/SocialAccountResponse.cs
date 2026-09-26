using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.SocialAccounts.ListSocialAccounts;

/// <summary>
///     Ответ с данными аккаунта соцсети
/// </summary>
public sealed record SocialAccountResponse
{
	public required Guid Id { get; init; }
	public required SocialPlatform Platform { get; init; }
	public required string Name { get; init; }
	public required SocialAccountStatus Status { get; init; }
	public DateTimeOffset? TokenExpiresAt { get; init; }
	public string? LastError { get; init; }
	public required DateTimeOffset Created { get; init; }
}
