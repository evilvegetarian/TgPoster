using Microsoft.AspNetCore.Mvc;
using Shared.Utilities;
using TgPoster.API.Models;

namespace TgPoster.API.Controllers;

/// <summary>
///     Общий роут для всех опций
/// </summary>
[ApiController]
[Route("api/options")]
public class OptionsController : ControllerBase
{
	/// <summary>
	///     Получить список статусов сообщений
	/// </summary>
	/// <returns>Список статусов сообщений с их названиями</returns>
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

	/// <summary>
	///     Получить список полей сортировки сообщений
	/// </summary>
	/// <returns>Список полей для сортировки сообщений</returns>
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

	/// <summary>
	///     Получить список направлений сортировки
	/// </summary>
	/// <returns>Список направлений сортировки (по возрастанию/убыванию)</returns>
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

	/// <summary>
	///     Получить список типов файлов
	/// </summary>
	/// <returns>Список поддерживаемых типов файлов</returns>
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