using System.ComponentModel.DataAnnotations;
using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.ChannelCleanup.PreviewChannelCleanup;

/// <summary>
/// Канал или группа, из которых аккаунт выйдет при очистке
/// </summary>
public sealed record ChannelCleanupCandidateResponse
{
	/// <summary>
	/// Telegram-id чата (без префикса -100)
	/// </summary>
	[Required]
	public required long ChatId { get; init; }

	/// <summary>
	/// Название
	/// </summary>
	[Required]
	public required string Title { get; init; }

	/// <summary>
	/// Username без @, у приватных чатов нет
	/// </summary>
	public string? Username { get; init; }

	/// <summary>
	/// Канал или группа
	/// </summary>
	[Required]
	public required ChatType ChatType { get; init; }
}

/// <summary>
/// Предпросмотр очистки: из чего аккаунт выйдет и сколько чатов останется
/// </summary>
public sealed record ChannelCleanupPreviewResponse
{
	/// <summary>
	/// Сколько всего каналов и групп у аккаунта
	/// </summary>
	[Required]
	public required int TotalCount { get; init; }

	/// <summary>
	/// Сколько остаётся, потому что их использует приложение (репост, парсинг, комментарии, расписания)
	/// </summary>
	[Required]
	public required int InUseCount { get; init; }

	/// <summary>
	/// Сколько остаётся, потому что аккаунт в них создатель или администратор
	/// </summary>
	[Required]
	public required int AdminCount { get; init; }

	/// <summary>
	/// Каналы и группы, из которых аккаунт выйдет
	/// </summary>
	[Required]
	public required List<ChannelCleanupCandidateResponse> Candidates { get; init; }
}
