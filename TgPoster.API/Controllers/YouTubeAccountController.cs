using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.YouTubeAccount.CallBackYouTube;
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
	/// <param name="request"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
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
	/// <param name="code"></param>
	/// <param name="state">Возвращает id записи в бд</param>
	/// <param name="error"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
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
	/// <param name="messageId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
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
	/// <param name="ct"></param>
	/// <returns></returns>
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
}
