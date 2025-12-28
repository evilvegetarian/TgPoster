using Microsoft.AspNetCore.Mvc;
using Shared;
using TgPoster.API.Models;

namespace TgPoster.API.Controllers;

/// <summary>
///     Общий роут для всех опций
/// </summary>
[ApiController]
[Route("api/options")]
public class OptionsController : ControllerBase
{
	[HttpGet("message-statuses")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<EnumViewModel<MessageStatus>>))]
	public IActionResult GetMessageStatuses()
	{
		var statuses = Enum.GetValues<MessageStatus>()
			.Select(status => new EnumViewModel<MessageStatus>
			{
				Value = status,
				Name = status.GetName()
			})
			.ToList();

		return Ok(statuses);
	}

	[HttpGet("message-sort-fields")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<EnumViewModel<MessageSortBy>>))]
	public IActionResult GetMessageSortFields()
	{
		var sortFields = Enum.GetValues<MessageSortBy>()
			.Select(field => new EnumViewModel<MessageSortBy>
			{
				Value = field,
				Name = field.GetName()
			})
			.ToList();

		return Ok(sortFields);
	}

	[HttpGet("sort-directions")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<EnumViewModel<SortDirection>>))]
	public IActionResult GetSortDirections()
	{
		var directions = Enum.GetValues<SortDirection>()
			.Select(dir => new EnumViewModel<SortDirection>
			{
				Value = dir,
				Name = dir.GetName()
			})
			.ToList();

		return Ok(directions);
	}

	[HttpGet("file-type")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<EnumViewModel<FileTypes>>))]
	public IActionResult GetFileTypes()
	{
		var directions = Enum.GetValues<FileTypes>()
			.Select(dir => new EnumViewModel<FileTypes>
			{
				Value = dir,
				Name = dir.GetName()
			})
			.ToList();

		return Ok(directions);
	}

	/// <summary>
	///     Модель для представления значения Enum на фронтенде.
	/// </summary>
	public class EnumViewModel<T>
	{
		/// <summary>
		///     Числовое значение элемента Enum.
		/// </summary>
		public required T Value { get; set; }

		/// <summary>
		///     Имя элемента Enum (для отображения пользователю).
		/// </summary>
		public required string Name { get; set; }
	}
}