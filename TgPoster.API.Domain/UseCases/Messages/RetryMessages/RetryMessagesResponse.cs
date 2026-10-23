namespace TgPoster.API.Domain.UseCases.Messages.RetryMessages;

/// <summary>
/// Результат повтора отправки постов
/// </summary>
public sealed record RetryMessagesResponse
{
	/// <summary>
	/// Посты и их новое время публикации
	/// </summary>
	public required List<RetriedMessage> Items { get; init; }
}

/// <summary>
/// Пост, поставленный на повторную отправку
/// </summary>
public sealed record RetriedMessage
{
	/// <summary>
	/// Id поста
	/// </summary>
	public required Guid Id { get; init; }

	/// <summary>
	/// Новое время публикации
	/// </summary>
	public required DateTimeOffset TimePosting { get; init; }
}
