namespace TgPoster.API.Domain.UseCases.CrossPostTargets.PreviewCrossPost;

/// <summary>
///     Данные расписания для предпросмотра кросс-поста
/// </summary>
public sealed class CrossPostPreviewContext
{
	public required string ChannelName { get; init; }
	public string? LastMessageText { get; init; }
	public required List<CrossPostPreviewTargetDto> Targets { get; init; }
}
