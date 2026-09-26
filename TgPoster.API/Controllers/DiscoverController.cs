using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Discover.GetCategories;
using TgPoster.API.Domain.UseCases.Discover.GetClassificationHistory;
using TgPoster.API.Domain.UseCases.Discover.GetClassificationStats;
using TgPoster.API.Domain.UseCases.Discover.GetClassificationStatus;
using TgPoster.API.Domain.UseCases.Discover.GetClassifierSettings;
using TgPoster.API.Domain.UseCases.Discover.GetDiscoverParseHistory;
using TgPoster.API.Domain.UseCases.Discover.GetDiscoverStats;
using TgPoster.API.Domain.UseCases.Discover.GetDiscoverStatus;
using TgPoster.API.Domain.UseCases.Discover.ListDiscover;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;
using TgPoster.API.Mapper;
using TgPoster.API.Models;

namespace TgPoster.API.Controllers;

/// <summary>
///     Контроллер для просмотра обнаруженных Telegram-каналов
/// </summary>
[Authorize]
[ApiController]
[Tags("Discover")]
public class DiscoverController(ISender sender) : ControllerBase
{
	/// <summary>
	///     Получить список обнаруженных каналов с пагинацией и фильтрацией
	/// </summary>
	[HttpGet(Routes.Discover.List)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponse<DiscoverChannelResponse>))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> List([FromQuery] ListDiscoverRequest request, CancellationToken ct)
	{
		var response = await sender.Send(request.ToDomain(), ct);
		return Ok(response);
	}

	/// <summary>
	///     Получить список тематик обнаруженных каналов
	/// </summary>
	[HttpGet(Routes.Discover.Categories)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<string>))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> GetCategories(CancellationToken ct)
	{
		var categories = await sender.Send(new GetCategoriesQuery(), ct);
		return Ok(categories);
	}

	/// <summary>
	///     Получить состояние фоновой задачи обнаружения каналов
	/// </summary>
	[HttpGet(Routes.Discover.Status)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DiscoverStatusResponse))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> GetStatus(CancellationToken ct)
	{
		var status = await sender.Send(new GetDiscoverStatusQuery(), ct);
		return Ok(status);
	}

	/// <summary>
	///     Получить статистику по обнаруженным каналам: итоги, свежесть, разбивки и таймлайны по дням
	/// </summary>
	[HttpGet(Routes.Discover.Stats)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DiscoverStatsResponse))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> GetStats([FromQuery] GetDiscoverStatsRequest request, CancellationToken ct)
	{
		var stats = await sender.Send(new GetDiscoverStatsQuery(request.Days), ct);
		return Ok(stats);
	}

	/// <summary>
	///     Получить историю парсинга: какие каналы и когда парсились, от самых свежих к старым
	/// </summary>
	[HttpGet(Routes.Discover.ParseHistory)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponse<DiscoverParseHistoryItemResponse>))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> GetParseHistory(
		[FromQuery] ListDiscoverParseHistoryRequest request,
		CancellationToken ct
	)
	{
		var response = await sender.Send(request.ToDomain(), ct);
		return Ok(response);
	}

	/// <summary>
	///     Получить статистику классификации каналов: покрытие, свежесть, уверенность модели и разбивки
	/// </summary>
	[HttpGet(Routes.Discover.ClassificationStats)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ClassificationStatsResponse))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> GetClassificationStats(
		[FromQuery] GetDiscoverStatsRequest request,
		CancellationToken ct
	)
	{
		var stats = await sender.Send(new GetClassificationStatsQuery(request.Days), ct);
		return Ok(stats);
	}

	/// <summary>
	///     Получить историю классификации: какие каналы и как классифицированы, от самых свежих к старым
	/// </summary>
	[HttpGet(Routes.Discover.ClassificationHistory)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponse<ClassificationHistoryItemResponse>))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> GetClassificationHistory(
		[FromQuery] ListClassificationHistoryRequest request,
		CancellationToken ct
	)
	{
		var response = await sender.Send(request.ToDomain(), ct);
		return Ok(response);
	}

	/// <summary>
	///     Получить состояние фоновой задачи классификации каналов
	/// </summary>
	[HttpGet(Routes.Discover.ClassificationStatus)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DiscoverStatusResponse))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> GetClassificationStatus(CancellationToken ct)
	{
		var status = await sender.Send(new GetClassificationStatusQuery(), ct);
		return Ok(status);
	}

	/// <summary>
	///     Получить настройки классификатора каналов вместе со значениями по умолчанию
	/// </summary>
	[HttpGet(Routes.Discover.ClassificationSettings)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ClassifierSettingsResponse))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> GetClassifierSettings(CancellationToken ct)
	{
		var settings = await sender.Send(new GetClassifierSettingsQuery(), ct);
		return Ok(settings);
	}

	/// <summary>
	///     Сохранить настройки классификатора каналов
	/// </summary>
	[HttpPut(Routes.Discover.ClassificationSettings)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> UpdateClassifierSettings(
		[FromBody] [Required] UpdateClassifierSettingsRequest request,
		CancellationToken ct
	)
	{
		await sender.Send(request.ToDomain(), ct);
		return NoContent();
	}
}