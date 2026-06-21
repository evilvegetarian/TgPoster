using MediatR;
using Shared.Telegram;
using TgPoster.API.Domain.Services;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.Files.GetFile;

internal sealed class GetFileUseCase(
	IFileStorage fileStorage,
	TelegramTokenService telegramTokenService,
	ITelegramService telegramService,
	TelegramBotManager botManager) : IRequestHandler<GetFileCommand, GetFileResponse>
{
	public async Task<GetFileResponse> Handle(GetFileCommand request, CancellationToken ct)
	{
		var fileInfo = await fileStorage.GetFileInfoAsync(request.FileId, ct);
		if (fileInfo is null)
		{
			throw new FileNotExistException();
		}

		var (token, _) = await telegramTokenService.GetTokenByMessageIdAsync(fileInfo.MessageId, ct);
		var botClient = botManager.GetClient(token);

		var fileData = await telegramService.GetByteFileAsync(botClient, fileInfo.TgFileId, ct);

		return new GetFileResponse(fileData, fileInfo.ContentType, $"{request.FileId}");
	}
}