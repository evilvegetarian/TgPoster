using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.CrossPostTargets.PreviewCrossPost;

/// <summary>
///     Предпросмотр кросс-поста для одного аккаунта
/// </summary>
public sealed record CrossPostPreviewResponse
{
	public required Guid SocialAccountId { get; init; }
	public required SocialPlatform Platform { get; init; }
	public required string AccountName { get; init; }
	public required CrossPostFormat Format { get; init; }
	public required List<CrossPostPreviewPart> Parts { get; init; }
	public required List<string> Warnings { get; init; }
}
