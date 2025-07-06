using MediatR;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Accounts.RefreshToken;
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
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpPost(Routes.Account.SignOn)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SignOnResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> SignOn([FromBody] SignOnRequest request, CancellationToken ct)
    {
        var response = await sender.Send(new SignOnCommand(request.Login, request.Password), ct);
        return Ok(response);
    }

    /// <summary>
    ///     Зайти в аккаунт
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpPost(Routes.Account.SignIn)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SignInResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> SignIn([FromBody] SignInRequest request, CancellationToken ct)
    {
        var response = await sender.Send(new SignInCommand(request.Login, request.Password), ct);
        return Ok(response);
    }

    /// <summary>
    ///     Обновить токен
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpPost(Routes.Account.RefreshToken)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RefreshTokenResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var response = await sender.Send(new RefreshTokenCommand(request.RefreshToken), ct);
        return Ok(response);
    }
}
