using MediatR;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Accounts.SignIn;
using TgPoster.API.Domain.UseCases.Accounts.SignOn;
using TgPoster.API.Models;

namespace TgPoster.API.Controllers;

[ApiController]
public class AccountController(ISender sender) : ControllerBase
{
    /// <summary>
    ///     Регистрация пользователя
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost(Routes.Account.SignOn)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SignOnResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> SignOn(
        [FromBody] SignOnRequest request,
        CancellationToken cancellationToken
    )
    {
        var response = await sender.Send(new SignOnCommand(request.Login, request.Password), cancellationToken);
        return Ok(response);
    }

    /// <summary>
    ///     Зайти в аккаунт
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost(Routes.Account.SignIn)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SignInResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SignIn([FromBody] SignInRequest request, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new SignInCommand(request.Login, request.Password), cancellationToken);
        return Ok(response);
    }
}