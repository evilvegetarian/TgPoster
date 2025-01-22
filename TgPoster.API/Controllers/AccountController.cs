using MediatR;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.Domain.UseCases.SignOn;

namespace TgPoster.API.Controllers;

[ApiController]
public class AccountController(ISender sender) : ControllerBase
{
    [HttpPost("sign-on")]
    public async Task<IActionResult> SignOn(string login, string password, CancellationToken cancellationToken)
    {
        await sender.Send(new SignOnCommand(login, password), cancellationToken);
        return Ok();
    }
}