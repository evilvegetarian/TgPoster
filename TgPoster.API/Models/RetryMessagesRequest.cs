using System.ComponentModel.DataAnnotations;
using TgPoster.API.Domain.UseCases.Messages.RetryMessages;

namespace TgPoster.API.Models;

/// <summary>
/// Повтор отправки упавших постов
/// </summary>
public sealed class RetryMessagesRequest
{
	/// <summary>
	/// Id постов, отправка которых завершилась ошибкой
	/// </summary>
	[Required(ErrorMessage = "Необходимо выбрать хотя бы один пост")]
	[MinLength(1, ErrorMessage = "Необходимо выбрать хотя бы один пост")]
	[MaxLength(1000, ErrorMessage = "За один раз можно повторить не больше 1000 постов")]
	public required List<Guid> Ids { get; set; }

	/// <summary>
	/// Когда отправить: сейчас или в ближайший свободный слот расписания
	/// </summary>
	[Required]
	public required RetryMode Mode { get; set; }
}
