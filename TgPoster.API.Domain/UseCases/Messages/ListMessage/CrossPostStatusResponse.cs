using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.Messages.ListMessage;

/// <summary>
///     Статус публикации поста в соцсети
/// </summary>
public sealed record CrossPostStatusResponse
{
	/// <summary>
	///     Id кросс-поста
	/// </summary>
	public required Guid Id { get; init; }

	/// <summary>
	///     Площадка
	/// </summary>
	public required SocialPlatform Platform { get; init; }

	/// <summary>
	///     Имя аккаунта для показа
	/// </summary>
	public required string AccountName { get; init; }

	/// <summary>
	///     Статус публикации
	/// </summary>
	public required CrossPostStatus Status { get; init; }

	/// <summary>
	///     Ссылка на пост в соцсети
	/// </summary>
	public string? ExternalUrl { get; init; }

	/// <summary>
	///     Ошибка или причина пропуска
	/// </summary>
	public string? Error { get; init; }

	/// <summary>
	///     Когда опубликовано
	/// </summary>
	public DateTimeOffset? PublishedAt { get; init; }
}
