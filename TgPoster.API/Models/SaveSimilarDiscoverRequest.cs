using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

/// <summary>Каналы, выбранные из рекомендаций Telegram</summary>
public sealed class SaveSimilarDiscoverRequest
{
	/// <summary>Идентификаторы выбранных каналов в Telegram</summary>
	[Required, MinLength(1)]
	public required List<long> TelegramIds { get; init; }
}
