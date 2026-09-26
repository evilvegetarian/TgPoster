using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.CrossPostTargets.PreviewCrossPost;

/// <summary>
///     Настройки аккаунта для предпросмотра кросс-поста
/// </summary>
public sealed class CrossPostPreviewTargetDto
{
	public required Guid SocialAccountId { get; init; }
	public required SocialPlatform Platform { get; init; }
	public required string AccountName { get; init; }
	public required CrossPostFormat Format { get; init; }
	public required CrossPostLinkTarget LinkTarget { get; init; }
	public string? CustomLink { get; init; }
	public string? CallToAction { get; init; }
}
