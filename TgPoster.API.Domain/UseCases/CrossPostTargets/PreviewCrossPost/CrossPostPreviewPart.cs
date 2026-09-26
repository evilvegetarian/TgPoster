namespace TgPoster.API.Domain.UseCases.CrossPostTargets.PreviewCrossPost;

/// <summary>
///     Одна часть предпросмотра кросс-поста
/// </summary>
public sealed record CrossPostPreviewPart
{
	public required string Text { get; init; }
	public required int Length { get; init; }
	public required int Limit { get; init; }
}
