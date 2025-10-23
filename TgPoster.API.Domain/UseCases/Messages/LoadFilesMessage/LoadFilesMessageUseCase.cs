using MediatR;
using Security.Interfaces;
using Telegram.Bot;
using TgPoster.API.Domain.ConfigModels;
using TgPoster.API.Domain.Services;

namespace TgPoster.API.Domain.UseCases.Messages.LoadFilesMessage;

internal class LoadFilesMessageUseCase(
	ILoadFilesMessageStorage storage,
	TelegramService telegramService,
	ICryptoAES cryptoAes,
	TelegramOptions options,
	TelegramTokenService tokenService,
	IIdentityProvider provider)
	: IRequestHandler<LoadFilesMessageCommand>
{
	public async Task Handle(LoadFilesMessageCommand request, CancellationToken ct)
	{
		var (token, chatId) = await tokenService.GetTokenByMessageIdAsync(request.Id, ct);

		var bot = new TelegramBotClient(token);
		var files = await telegramService.GetFileMessageInTelegramByFile(bot, request.Files, chatId, ct);

		await storage.AddFileAsync(request.Id, files, ct);
	}
}