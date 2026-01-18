using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.YouTubeAccount.CallBackYouTube;
using TgPoster.API.Domain.UseCases.YouTubeAccount.DeleteYouTubeAccount;
using TgPoster.API.Domain.UseCases.YouTubeAccount.GetYouTubeAccounts;
using TgPoster.API.Domain.UseCases.YouTubeAccount.SendVideoOnYouTube;
using TgPoster.API.Domain.UseCases.YouTubeAccount.YouTubeAccountLogin;
using TgPoster.API.Models;

namespace TgPoster.API.Controllers;

/// <summary>
///     Контроллер Ютуб аккаунта
/// </summary>
/// <param name="sender"></param>
[Authorize]
[ApiController]
public class YouTubeAccountController(ISender sender) : ControllerBase
{
	/// <summary>
	///     Регистрация Аккаунта, редиректит на гугл авторизацию
	/// </summary>
	/// <param name="request">Данные для регистрации аккаунта YouTube (JSON-файл, идентификатор клиента и секрет клиента)</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>URL для авторизации через Google</returns>
	[HttpPost(Routes.YouTubeAccount.Create)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> AuthYouTube(
		[FromForm] LoginYouTubeRequest request,
		CancellationToken ct
	)
	{
		var uri = "http://localhost:5173/api/v1/youtube/callback";
		var command = new LoginYouTubeCommand(request.JsonFile, request.ClientId, request.ClientSecret, uri);
		var authUrl = await sender.Send(command, ct);
		return Ok(new { Url = authUrl });
	}

	/// <summary>
	///     Коллбэк от гугл обратно к нам
	/// </summary>
	/// <param name="code">Код авторизации от Google</param>
	/// <param name="state">Идентификатор записи в базе данных</param>
	/// <param name="error">Сообщение об ошибке (если есть)</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Результат выполнения операции</returns>
	[AllowAnonymous]
	[HttpGet(Routes.YouTubeAccount.CallBack)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> GoogleCallback(string code, string state, string? error, CancellationToken ct)
	{
		var uri = "http://localhost:5173/api/v1/youtube/callback";
		var command = new CallBackYouTubeQuery(code, state, uri);
		await sender.Send(command, ct);
		return Ok();
	}

	/// <summary>
	///     Отправка сообщения с видео в ютуб
	/// </summary>
	/// <param name="messageId">Идентификатор сообщения с видео для отправки</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Результат выполнения операции</returns>
	[HttpPost(Routes.YouTubeAccount.SendVideo)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> SendVideoInYoutube(Guid messageId, CancellationToken ct)
	{
		var command = new SendVideoOnYouTubeCommand(messageId);
		await sender.Send(command, ct);
		return Ok();
	}

	/// <summary>
	///     Получение списка ютуб аккаунтов
	/// </summary>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Список всех YouTube аккаунтов пользователя</returns>
	[HttpGet(Routes.YouTubeAccount.List)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<YouTubeAccountResponse>))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> GetYouTubeAccounts(CancellationToken ct)
	{
		var query = new GetYouTubeAccountsQuery();
		var result = await sender.Send(query, ct);
		return Ok(result);
	}

	/// <summary>
	///     Удаление YouTube аккаунта
	/// </summary>
	/// <param name="id">Идентификатор YouTube аккаунта для удаления</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Результат выполнения операции</returns>
	[HttpDelete(Routes.YouTubeAccount.Delete)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
	{
		await sender.Send(new DeleteYouTubeAccountCommand(id), ct);
		return Ok();
	}
}
