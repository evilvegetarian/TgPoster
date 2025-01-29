using MediatR;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Models;
using TgPoster.Domain.UseCases.Accounts.SignIn;
using TgPoster.Domain.UseCases.Accounts.SignOn;

namespace TgPoster.API.Controllers;

[ApiController]
public class AccountController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Регистрация пользователя
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost(Routes.Account.SignOn)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SignOnResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SignOn(
        [FromBody] SignOnRequest request,
        CancellationToken cancellationToken
    )
    {
        var userId = await sender.Send(new SignOnCommand(request.Login, request.Password), cancellationToken);
        var response = new SignOnResponse
        {
            UserId = userId
        };
        return Ok(response);
    }

    /// <summary>
    /// Зайти в аккаунт
    /// </summary>
    /// <param name="login"></param>
    /// <param name="password"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost(Routes.Account.SignIn)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SignInResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SignIn(string login, string password, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new SignInCommand(login, password), cancellationToken);
        return Ok(response);
    }
}
