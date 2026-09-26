namespace TgPoster.Worker.Domain.UseCases.CrossPosting.Media;

/// <summary>
///     Загружает и обрабатывает медиа для кросс-поста
/// </summary>
internal interface ICrossPostMediaLoader
{
	/// <summary>
	///     Загрузить изображения поста
	/// </summary>
	/// <param name="botToken"></param>
	/// <param name="files"></param>
	/// <param name="profile"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<IReadOnlyList<LoadedImage>> LoadAsync(
		string botToken,
		IReadOnlyList<CrossPostFileDto> files,
		MediaProfile profile,
		CancellationToken ct);
}
