namespace TgPoster.Worker.Domain.UseCases.CrossPosting.Publishing;

/// <summary>
///     Вид ошибки публикации
/// </summary>
internal enum SocialPublishErrorKind
{
	None,
	Transient,
	Auth,
	Permanent
}
