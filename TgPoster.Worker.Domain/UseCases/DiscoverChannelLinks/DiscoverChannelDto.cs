namespace TgPoster.Worker.Domain.UseCases.DiscoverChannelLinks;

public sealed class DiscoverChannelDto
{
	public string? Username { get; set; }
	public long? TelegramId { get; init; }
	public string? InviteHash { get; init; }
	public int? LastParsedId { get; init; }
}
