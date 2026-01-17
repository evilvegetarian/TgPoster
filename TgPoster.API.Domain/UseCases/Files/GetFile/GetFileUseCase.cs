using MediatR;
using Telegram.Bot;
using TgPoster.API.Domain.Exceptions;
using TgPoster.API.Domain.Services;

namespace TgPoster.API.Domain.UseCases.Files.GetFile;

internal sealed class GetFileUseCase(
	IFileStorage fileStorage,
	TelegramTokenService telegramTokenService,
	TelegramService telegramService) : IRequestHandler<GetFileCommand, GetFileResponse>
{
	public async Task<GetFileResponse> Handle(GetFileCommand request, CancellationToken ct)
	{
		var fileInfo = await fileStorage.GetFileInfoAsync(request.FileId, ct);
		if (fileInfo is null)
		{
			throw new FileNotExistException();
		}

		var (token, _) = await telegramTokenService.GetTokenByMessageIdAsync(fileInfo.MessageId, ct);
		var botClient = new TelegramBotClient(token);

		var fileData = await telegramService.GetByteFileAsync(botClient, fileInfo.TgFileId, ct);

		return new GetFileResponse(fileData, fileInfo.ContentType, $"{request.FileId}");
	}
}