using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

/// <summary>
/// Запуск выхода из каналов, подтверждённых в предпросмотре
/// </summary>
public sealed class StartChannelCleanupRequest
{
	/// <summary>
	/// Telegram-id каналов и групп из предпросмотра. Выход будет только из тех, что по-прежнему не используются
	/// </summary>
	[Required(ErrorMessage = "Необходимо выбрать хотя бы один канал")]
	[MinLength(1, ErrorMessage = "Необходимо выбрать хотя бы один канал")]
	[MaxLength(2000, ErrorMessage = "Слишком много каналов за один раз")]
	public required List<long> ChatIds { get; set; }
}
