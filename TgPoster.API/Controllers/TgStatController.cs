using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.TgStat.ScrapeChannels;

namespace TgPoster.API.Controllers;

/// <summary>
///     Контроллер скрейпинга каналов с TGStat.
/// </summary>
[ApiController]
public sealed class TgStatController(ISender sender) : ControllerBase
{
	/// <summary>
	///     Запуск скрейпинга каналов по URL-ам TGStat.
	/// </summary>
	/// <param name="command">Массив URL-ов каналов на TGStat.</param>
	/// <param name="ct">Токен отмены.</param>
	[HttpPost(Routes.TgStat.ScrapeChannels)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public async Task<IActionResult> ScrapeChannels(
		[FromBody] ScrapeChannelsCommand command,
		CancellationToken ct)
	{
		await sender.Send(command, ct);
		return NoContent();
	}
}
