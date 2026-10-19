namespace TgPoster.Worker.Domain.UseCases.DiscoverChannelLinks;

public sealed class DiscoverChannelDto
{
	public Guid Id { get; init; }
	public string? Username { get; set; }
	public long? TelegramId { get; init; }
	public string? InviteHash { get; init; }
	public int? LastParsedId { get; init; }

	/// <summary>
	/// Сессии, у которых в этом канале записан бан: канал им не раздаётся
	/// </summary>
	public IReadOnlyList<Guid> BannedSessionIds { get; init; } = [];
}
