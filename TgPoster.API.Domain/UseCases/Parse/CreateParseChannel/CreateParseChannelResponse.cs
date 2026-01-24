namespace TgPoster.API.Domain.UseCases.Parse.CreateParseChannel;

public sealed record CreateParseChannelResponse
{
	public required Guid Id { get; init; }
}