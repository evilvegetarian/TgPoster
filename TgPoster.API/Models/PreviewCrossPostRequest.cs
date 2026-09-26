using System.ComponentModel.DataAnnotations;
using Shared.Enums;

namespace TgPoster.API.Models;

/// <summary>
///     Запрос на предпросмотр кросс-поста
/// </summary>
public sealed class PreviewCrossPostRequest : IValidatableObject
{
	/// <summary>
	///     Идентификатор аккаунта соцсети, null — все связки расписания
	/// </summary>
	public Guid? SocialAccountId { get; set; }

	/// <summary>
	///     Переопределение формата кросс-поста
	/// </summary>
	public CrossPostFormat? Format { get; set; }

	/// <summary>
	///     Переопределение цели ссылки
	/// </summary>
	public CrossPostLinkTarget? LinkTarget { get; set; }

	/// <summary>
	///     Переопределение своей ссылки
	/// </summary>
	[StringLength(512)]
	public string? CustomLink { get; set; }

	/// <summary>
	///     Переопределение вариантов призыва
	/// </summary>
	[StringLength(1000)]
	public string? CallToAction { get; set; }

	/// <summary>
	///     Текст поста, null — текст последнего поста расписания с непустым текстом
	/// </summary>
	[StringLength(4096)]
	public string? Text { get; set; }

	/// <summary>
	///     Валидация запроса
	/// </summary>
	/// <param name="validationContext"></param>
	/// <returns></returns>
	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		return CrossPostTargetRequestValidation.Validate(LinkTarget, CustomLink, CallToAction);
	}
}
