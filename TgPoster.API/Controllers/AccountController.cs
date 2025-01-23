using MediatR;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.Domain.UseCases.SignOn;

namespace TgPoster.API.Controllers;

[ApiController]
public class AccountController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Регистрация пользователя
    /// </summary>
    /// <param name="login"></param>
    /// <param name="password"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("sign-on")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SignOn(string login, string password, CancellationToken cancellationToken)
    {
        var userId = await sender.Send(new SignOnCommand(login, password), cancellationToken);
        return Ok(userId);
    }
}