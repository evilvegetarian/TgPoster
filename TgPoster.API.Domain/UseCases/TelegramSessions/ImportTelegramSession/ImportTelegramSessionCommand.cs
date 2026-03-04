using MediatR;
using Microsoft.AspNetCore.Http;

namespace TgPoster.API.Domain.UseCases.TelegramSessions.ImportTelegramSession;

/// <summary>
///     Команда импорта Telegram сессии из файла
/// </summary>
public sealed record ImportTelegramSessionCommand(
	string ApiId,
	string ApiHash,
	IFormFile SessionFile,
	string? Name
) : IRequest<ImportTelegramSessionResponse>;
