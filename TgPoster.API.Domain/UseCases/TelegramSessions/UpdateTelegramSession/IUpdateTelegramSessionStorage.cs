namespace TgPoster.API.Domain.UseCases.TelegramSessions.UpdateTelegramSession;

public interface IUpdateTelegramSessionStorage
{
	Task<TelegramSessionDto?> GetByIdAsync(Guid userId, Guid sessionId, CancellationToken ct);
	Task UpdateAsync(Guid sessionId, string? name, bool isActive, CancellationToken ct);
}

public sealed class TelegramSessionDto
{
	public required Guid Id { get; init; }
	public required string? Name { get; init; }
	public required bool IsActive { get; init; }
}
