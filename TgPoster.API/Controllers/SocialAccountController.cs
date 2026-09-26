using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.SocialAccounts.ConnectBluesky;
using TgPoster.API.Domain.UseCases.SocialAccounts.DeleteSocialAccount;
using TgPoster.API.Domain.UseCases.SocialAccounts.ListSocialAccounts;
using TgPoster.API.Models;

namespace TgPoster.API.Controllers;

/// <summary>
///     Контроллер аккаунтов соцсетей
/// </summary>
/// <param name="sender"></param>
[Authorize]
[ApiController]
[Tags("SocialAccount")]
public class SocialAccountController(ISender sender) : ControllerBase
{
	/// <summary>
	///     Получение списка аккаунтов соцсетей текущего пользователя
	/// </summary>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Список аккаунтов соцсетей</returns>
	[HttpGet(Routes.SocialAccount.List)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<SocialAccountResponse>))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> GetSocialAccounts(CancellationToken ct)
	{
		var result = await sender.Send(new ListSocialAccountsQuery(), ct);
		return Ok(result);
	}

	/// <summary>
	///     Подключение аккаунта Bluesky
	/// </summary>
	/// <param name="request">Данные для входа в Bluesky</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Идентификатор подключённого аккаунта</returns>
	[HttpPost(Routes.SocialAccount.ConnectBluesky)]
	[ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ConnectSocialAccountResponse))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> ConnectBluesky([FromBody] ConnectBlueskyRequest request, CancellationToken ct)
	{
		var command = new ConnectBlueskyCommand(request.Handle, request.AppPassword);
		var response = await sender.Send(command, ct);
		return Created(Routes.SocialAccount.ConnectBluesky, response);
	}

	/// <summary>
	///     Удаление аккаунта соцсети
	/// </summary>
	/// <param name="id">Идентификатор аккаунта соцсети</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Результат удаления</returns>
	[HttpDelete(Routes.SocialAccount.Delete)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
	{
		await sender.Send(new DeleteSocialAccountCommand(id), ct);
		return NoContent();
	}
}
