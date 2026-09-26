namespace TgPoster.Worker.Domain.UseCases.CrossPosting.Publishing;

/// <summary>
///     Запрос на публикацию в соцсеть
/// </summary>
internal sealed class SocialPublishRequest
{
	/// <summary>
	///     Id кросс-поста
	/// </summary>
	public required Guid CrossPostId { get; init; }

	/// <summary>
	///     Id аккаунта соцсети
	/// </summary>
	public required Guid AccountId { get; init; }

	/// <summary>
	///     Имя аккаунта (handle/username)
	/// </summary>
	public required string AccountName { get; init; }

	/// <summary>
	///     Внешний id пользователя площадки
	/// </summary>
	public required string AccountExternalUserId { get; init; }

	/// <summary>
	///     Расшифрованный секрет аккаунта
	/// </summary>
	public required string AccountSecret { get; init; }

	/// <summary>
	///     Части текста: 1 — одиночный пост, N — цепочка
	/// </summary>
	public required IReadOnlyList<string> Parts { get; init; }

	/// <summary>
	///     Изображения, прикладываемые к первой части
	/// </summary>
	public required IReadOnlyList<Media.LoadedImage> Images { get; init; }

	/// <summary>
	///     URL ссылки на Telegram
	/// </summary>
	public string? LinkUrl { get; init; }

	/// <summary>
	///     Текст ссылки для площадки
	/// </summary>
	public string? LinkText { get; init; }
}
