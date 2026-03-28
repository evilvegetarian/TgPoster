namespace TgPoster.Worker.Domain.UseCases.DiscoverChannelLinks;

public sealed class DiscoverChannelDto
{
	public required string Username { get; set; }
	public int? LastParsedId { get; init; }
}
