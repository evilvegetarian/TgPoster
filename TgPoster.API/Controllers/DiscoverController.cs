using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Discover.GetCategories;
using TgPoster.API.Domain.UseCases.Discover.ListDiscover;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;
using TgPoster.API.Mapper;
using TgPoster.API.Models;

namespace TgPoster.API.Controllers;

/// <summary>
///     Контроллер для просмотра обнаруженных Telegram-каналов
/// </summary>
[Authorize]
[ApiController]
[Tags("Discover")]
public class DiscoverController(ISender sender) : ControllerBase
{
    /// <summary>
    ///     Получить список обнаруженных каналов с пагинацией и фильтрацией
    /// </summary>
    [HttpGet(Routes.Discover.List)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponse<DiscoverChannelResponse>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> List([FromQuery] ListDiscoverRequest request, CancellationToken ct)
    {
        var response = await sender.Send(request.ToDomain(), ct);
        return Ok(response);
    }

    /// <summary>
    ///     Получить список тематик обнаруженных каналов
    /// </summary>
    [HttpGet(Routes.Discover.Categories)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<string>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var categories = await sender.Send(new GetCategoriesQuery(), ct);
        return Ok(categories);
    }
}
