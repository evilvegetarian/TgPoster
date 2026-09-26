using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Domain.UseCases.Discover.GetClassificationHistory;

/// <summary>
///     Запись истории классификации: канал и что про него решила модель
/// </summary>
public sealed record ClassificationHistoryItemResponse
{
	/// <summary>ID канала</summary>
	public required Guid Id { get; init; }

	/// <summary>Username канала (без @)</summary>
	public string? Username { get; init; }

	/// <summary>Название канала</summary>
	public string? Title { get; init; }

	/// <summary>URL аватарки</summary>
	public string? AvatarUrl { get; init; }

	/// <summary>Ссылка на канал</summary>
	public string? TgUrl { get; init; }

	/// <summary>Число подписчиков</summary>
	public int? ParticipantsCount { get; init; }

	/// <summary>Тематика</summary>
	public string? Category { get; init; }

	/// <summary>Подкатегория</summary>
	public string? Subcategory { get; init; }

	/// <summary>Теги (пустой список, если модель их не вернула)</summary>
	[Required]
	public required string[] Tags { get; init; }

	/// <summary>Язык канала (ru, en, uk и т.д.)</summary>
	public string? Language { get; init; }

	/// <summary>Уверенность модели (0.0–1.0)</summary>
	public double? Confidence { get; init; }

	/// <summary>Когда канал классифицировался последний раз</summary>
	public required DateTimeOffset ClassifiedAt { get; init; }
}
