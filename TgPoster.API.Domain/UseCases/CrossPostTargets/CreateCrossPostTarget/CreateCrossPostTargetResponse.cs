namespace TgPoster.API.Domain.UseCases.CrossPostTargets.CreateCrossPostTarget;

/// <summary>
///     Ответ с идентификатором созданной связки
/// </summary>
public sealed record CreateCrossPostTargetResponse
{
	public required Guid Id { get; init; }
}
