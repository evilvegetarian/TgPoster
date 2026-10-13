using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.ChannelCleanup.GetChannelCleanupJob;
using TgPoster.API.Domain.UseCases.ChannelCleanup.PreviewChannelCleanup;
using TgPoster.API.Domain.UseCases.ChannelCleanup.StartChannelCleanup;
using TgPoster.API.Models;

namespace TgPoster.API.Controllers;

/// <summary>
/// Выход Telegram-аккаунта из каналов и групп, которые приложение не использует
/// </summary>
/// <param name="sender"></param>
[Authorize]
[ApiController]
[Tags("ChannelCleanup")]
public sealed class ChannelCleanupController(ISender sender) : ControllerBase
{
	/// <summary>
	/// Предпросмотр очистки: из каких каналов и групп аккаунт выйдет и сколько останется
	/// </summary>
	/// <param name="sessionId">ID Telegram-сессии</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Кандидаты на выход и число оставляемых чатов</returns>
	[HttpGet(Routes.ChannelCleanup.Preview)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ChannelCleanupPreviewResponse))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Preview([FromRoute] [Required] Guid sessionId, CancellationToken ct)
	{
		var result = await sender.Send(new PreviewChannelCleanupQuery(sessionId), ct);

		return Ok(result);
	}

	/// <summary>
	/// Запуск фонового выхода из каналов. Каналы обрабатываются по одному, прогресс опрашивается через GetJob
	/// </summary>
	/// <param name="sessionId">ID Telegram-сессии</param>
	/// <param name="request">Каналы, подтверждённые в предпросмотре</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Созданное задание</returns>
	[HttpPost(Routes.ChannelCleanup.Start)]
	[ProducesResponseType(StatusCodes.Status202Accepted, Type = typeof(ChannelCleanupJobResponse))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Start(
		[FromRoute] [Required] Guid sessionId,
		[FromBody] [Required] StartChannelCleanupRequest request,
		CancellationToken ct
	)
	{
		var result = await sender.Send(new StartChannelCleanupCommand(sessionId, request.ChatIds), ct);

		return Accepted(result);
	}

	/// <summary>
	/// Состояние задания на очистку каналов
	/// </summary>
	/// <param name="jobId">ID задания</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Прогресс задания и результат по каждому каналу</returns>
	[HttpGet(Routes.ChannelCleanup.GetJob)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ChannelCleanupJobResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> GetJob([FromRoute] [Required] Guid jobId, CancellationToken ct)
	{
		var result = await sender.Send(new GetChannelCleanupJobQuery(jobId), ct);

		return Ok(result);
	}
}
