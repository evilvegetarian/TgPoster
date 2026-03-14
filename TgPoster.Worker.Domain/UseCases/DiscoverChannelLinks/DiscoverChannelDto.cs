namespace TgPoster.Worker.Domain.UseCases.DiscoverChannelLinks;

public sealed class DiscoverChannelDto
{
	public required string Username { get; init; }
	public int? LastParsedId { get; init; }
}
