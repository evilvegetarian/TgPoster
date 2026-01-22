using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.TelegramSessions.CreateTelegramSession;
using TgPoster.API.Domain.UseCases.TelegramSessions.DeleteTelegramSession;
using TgPoster.API.Domain.UseCases.TelegramSessions.ListTelegramSessions;
using TgPoster.API.Domain.UseCases.TelegramSessions.SendPassword;
using TgPoster.API.Domain.UseCases.TelegramSessions.StartAuth;
using TgPoster.API.Domain.UseCases.TelegramSessions.UpdateTelegramSession;
using TgPoster.API.Domain.UseCases.TelegramSessions.VerifyCode;
using TgPoster.API.Models;

namespace TgPoster.API.Controllers;

/// <summary>
///     Управление Telegram сессиями
/// </summary>
[Authorize]
[ApiController]
public class TelegramSessionController(ISender sender) : ControllerBase
{
	/// <summary>
	///     Добавление Telegram сессии
	/// </summary>
	/// <param name="request">Данные для создания сессии (ApiId, ApiHash, PhoneNumber)</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Ответ с данными созданной Telegram сессии</returns>
	[HttpPost(Routes.TelegramSession.Create)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CreateTelegramSessionResponse))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Create(
		[FromBody] [Required] CreateTelegramSessionRequest request,
		CancellationToken ct
	)
	{
		var response = await sender.Send(
			new CreateTelegramSessionCommand(request.ApiId, request.ApiHash, request.PhoneNumber, request.Name), ct);
		return Ok(response);
	}

	/// <summary>
	///     Обновление Telegram сессии
	/// </summary>
	/// <param name="id">Идентификатор сессии для обновления</param>
	/// <param name="request">Данные для обновления сессии (имя и статус активности)</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Результат выполнения операции</returns>
	[HttpPut(Routes.TelegramSession.Update)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Update(
		[FromRoute] [Required] Guid id,
		[FromBody] [Required] UpdateTelegramSessionRequest request,
		CancellationToken ct
	)
	{
		await sender.Send(new UpdateTelegramSessionCommand(id, request.Name, request.IsActive), ct);
		return Ok();
	}

	/// <summary>
	///     Получение всех Telegram сессий пользователя
	/// </summary>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Список всех Telegram сессий текущего пользователя</returns>
	[HttpGet(Routes.TelegramSession.List)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<TelegramSessionResponse>))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> List(CancellationToken ct)
	{
		var response = await sender.Send(new ListTelegramSessionsQuery(), ct);
		return Ok(response);
	}

	/// <summary>
	///     Удаление Telegram сессии
	/// </summary>
	/// <param name="id">Идентификатор сессии для удаления</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Результат выполнения операции</returns>
	[HttpDelete(Routes.TelegramSession.Delete)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Delete([FromRoute] [Required] Guid id, CancellationToken ct)
	{
		await sender.Send(new DeleteTelegramSessionCommand(id), ct);
		return Ok();
	}

	/// <summary>
	///     Начать авторизацию Telegram сессии (отправка кода)
	/// </summary>
	/// <param name="id">Идентификатор сессии</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Статус отправки кода</returns>
	[HttpPost(Routes.TelegramSession.StartAuth)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StartAuthResponse))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> StartAuth([FromRoute] [Required] Guid id, CancellationToken ct)
	{
		var response = await sender.Send(new StartAuthCommand(id), ct);
		return Ok(response);
	}

	/// <summary>
	///     Проверка кода верификации
	/// </summary>
	/// <param name="id">Идентификатор сессии</param>
	/// <param name="request">Код верификации из Telegram</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Результат проверки кода</returns>
	[HttpPost(Routes.TelegramSession.VerifyCode)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VerifyCodeResponse))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> VerifyCode(
		[FromRoute] [Required] Guid id,
		[FromBody] [Required] VerifyCodeRequest request,
		CancellationToken ct
	)
	{
		var response = await sender.Send(new VerifyCodeCommand(id, request.Code), ct);
		return Ok(response);
	}

	/// <summary>
	///     Отправка пароля двухфакторной аутентификации
	/// </summary>
	/// <param name="id">Идентификатор сессии</param>
	/// <param name="request">Пароль 2FA</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Результат проверки пароля</returns>
	[HttpPost(Routes.TelegramSession.SendPassword)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SendPasswordResponse))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> SendPassword(
		[FromRoute] [Required] Guid id,
		[FromBody] [Required] SendPasswordRequest request,
		CancellationToken ct
	)
	{
		var response = await sender.Send(new SendPasswordCommand(id, request.Password), ct);
		return Ok(response);
	}
}