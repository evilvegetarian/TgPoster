using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Files;
using TgPoster.API.Domain.UseCases.Files.GetFile;
using TgPoster.API.Domain.UseCases.Files.UploadFileToS3;

namespace TgPoster.API.Controllers;

/// <summary>
///     Контроллер управления файлами
/// </summary>
/// <param name="sender"></param>
[Authorize]
[ApiController]
public class FileController(ISender sender) : ControllerBase
{
	/// <summary>
	///     Получение файла по Id
	/// </summary>
	/// <param name="id">Идентификатор файла</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Содержимое файла с соответствующим MIME-типом</returns>
	[HttpGet(Routes.File.Get)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileContentResult))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken ct)
	{
		var response = await sender.Send(new GetFileCommand(id), ct);
		return File(response.Data, response.ContentType);
	}

	/// <summary>
	///     Загрузка файла в S3 хранилище
	/// </summary>
	/// <param name="id">Идентификатор файла</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>URL файла в S3</returns>
	[HttpGet(Routes.File.UploadToS3)]
	[AllowAnonymous]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> UploadToS3([FromRoute] Guid id, CancellationToken ct)
	{
		var response = await sender.Send(new UploadFileToS3Command(id), ct);
		return Redirect(response);
	}
}