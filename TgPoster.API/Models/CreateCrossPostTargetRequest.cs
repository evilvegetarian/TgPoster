using System.ComponentModel.DataAnnotations;
using Shared.Enums;

namespace TgPoster.API.Models;

/// <summary>
///     Запрос на создание связки расписания с аккаунтом соцсети
/// </summary>
public sealed class CreateCrossPostTargetRequest : IValidatableObject
{
	/// <summary>
	///     Идентификатор аккаунта соцсети
	/// </summary>
	[Required]
	public required Guid SocialAccountId { get; set; }

	/// <summary>
	///     Формат кросс-поста
	/// </summary>
	[Required]
	public required CrossPostFormat Format { get; set; }

	/// <summary>
	///     Куда вести ссылку
	/// </summary>
	[Required]
	public required CrossPostLinkTarget LinkTarget { get; set; }

	/// <summary>
	///     Произвольная ссылка, обязательна при своей ссылке
	/// </summary>
	[StringLength(512)]
	public string? CustomLink { get; set; }

	/// <summary>
	///     Варианты призыва построчно
	/// </summary>
	[StringLength(1000)]
	public string? CallToAction { get; set; }

	/// <summary>
	///     Прикладывать медиа к кросс-посту
	/// </summary>
	[Required]
	public required bool IncludeMedia { get; set; }

	/// <summary>
	///     Кросс-постить посты, созданные парсером чужих каналов
	/// </summary>
	[Required]
	public required bool IncludeParsed { get; set; }

	/// <summary>
	///     Задержка публикации в минутах
	/// </summary>
	[Required]
	[Range(0, 1440)]
	public required int DelayMinutes { get; set; }

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
