using MediatR;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Mapper;
using TgPoster.API.Models;

namespace TgPoster.API.Controllers;

[ApiController]
public class ParseController(ISender sender) : ControllerBase
{
    [HttpPost(Routes.Parse.ParseChannel)]
    public async Task<IActionResult> ParseChannel(ParseChannelRequest request, CancellationToken ct)
    {
        await sender.Send(request.ToCommand(), ct);
        return Ok();
    }
}