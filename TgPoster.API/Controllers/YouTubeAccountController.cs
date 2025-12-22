using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.YouTubeAccount.CallBackYouTube;
using TgPoster.API.Domain.UseCases.YouTubeAccount.YouTubeAccountLogin;
using TgPoster.API.Models;

namespace TgPoster.API.Controllers;

/// <summary>
///     Контроллер Ютуб аккаунта
/// </summary>
/// <param name="sender"></param>
//[Authorize]
[ApiController]
public class YouTubeAccountController(ISender sender) : ControllerBase
{
	/// <summary>
	/// Регистрация Аккаунта, редиректит на гугл авторизацию
	/// </summary>
	/// <param name="request"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	[HttpPost(Routes.YouTubeAccount.Create)]
	public async Task<IActionResult> AuthYouTube(
		[FromForm] LoginYouTubeRequest request,
		CancellationToken ct
	)
	{
		var uri = Routes.YouTubeAccount.CallBack;
		var command = new LoginYouTubeCommand(request.JsonFile, request.ClientId, request.ClientSecret, uri);
		var authUrl = await sender.Send(command, ct);
		return Redirect(authUrl);
	}

	/// <summary>
	/// Коллбэк от гугл обратно к нам
	/// </summary>
	/// <param name="code"></param>
	/// <param name="state">Возвращает id записи в бд</param>
	/// <param name="error"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	[HttpGet(Routes.YouTubeAccount.CallBack)]
	public async Task<IActionResult> GoogleCallback(string code, string state, string? error, CancellationToken ct)
	{
		var uri = Routes.YouTubeAccount.CallBack;
		var command = new CallBackYouTubeQuery(code, state, uri);
		await sender.Send(command, ct);
		return Ok();
	}
}