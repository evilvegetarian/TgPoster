using System.ComponentModel.DataAnnotations;
using Shared.Enums;

namespace TgPoster.API.Models;

/// <summary>
///     Общая валидация запросов связок кросс-постинга
/// </summary>
internal static class CrossPostTargetRequestValidation
{
	/// <summary>
	///     Проверить свою ссылку и варианты призыва
	/// </summary>
	/// <param name="linkTarget"></param>
	/// <param name="customLink"></param>
	/// <param name="callToAction"></param>
	/// <returns></returns>
	internal static IEnumerable<ValidationResult> Validate(
		CrossPostLinkTarget? linkTarget,
		string? customLink,
		string? callToAction)
	{
		if (linkTarget == CrossPostLinkTarget.Custom)
		{
			if (string.IsNullOrWhiteSpace(customLink)
			    || !Uri.TryCreate(customLink, UriKind.Absolute, out var uri)
			    || uri.Scheme is not ("http" or "https"))
			{
				yield return new ValidationResult(
					"Для своей ссылки нужен полный адрес http(s)://…",
					["CustomLink"]);
			}
		}

		if (string.IsNullOrEmpty(callToAction))
		{
			yield break;
		}

		foreach (var line in callToAction.Split('\n'))
		{
			if (line.Trim().Length > 200)
			{
				yield return new ValidationResult(
					"Каждый вариант призыва — не длиннее 200 символов",
					["CallToAction"]);
				yield break;
			}
		}
	}
}
