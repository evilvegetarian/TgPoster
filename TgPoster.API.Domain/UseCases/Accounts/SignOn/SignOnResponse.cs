namespace TgPoster.API.Domain.UseCases.Accounts.SignOn;

public sealed record SignOnResponse
{
	public required Guid UserId { get; init; }
}