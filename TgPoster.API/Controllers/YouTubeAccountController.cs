using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
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
}