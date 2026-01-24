namespace TgPoster.API.Domain.UseCases.Messages.CreateMessage;

public sealed record CreateMessageResponse
{
	public required Guid Id { get; init; }
}