namespace TgPoster.Worker.Domain.UseCases.CrossPosting.Publishing;

/// <summary>
///     Результат публикации в соцсеть
/// </summary>
internal sealed record SocialPublishResult(
	bool IsSuccess,
	string? ExternalPostId,
	string? ExternalUrl,
	string? Error,
	SocialPublishErrorKind ErrorKind)
{
	public static SocialPublishResult Success(string externalPostId, string? externalUrl) =>
		new(true, externalPostId, externalUrl, null, SocialPublishErrorKind.None);

	public static SocialPublishResult Failure(SocialPublishErrorKind kind, string error, string? externalUrl = null) =>
		new(false, null, externalUrl, error, kind);
}
