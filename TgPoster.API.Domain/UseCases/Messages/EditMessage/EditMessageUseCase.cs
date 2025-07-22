using MediatR;
using Security.Interfaces;
using Telegram.Bot;
using TgPoster.API.Domain.ConfigModels;
using TgPoster.API.Domain.Exceptions;
using TgPoster.API.Domain.Services;

namespace TgPoster.API.Domain.UseCases.Messages.EditMessage;

internal class EditMessageUseCase(
    IEditMessageStorage storage,
    IIdentityProvider provider,
    ICryptoAES cryptoAes,
    TelegramOptions options,
    TelegramService telegramService)
    : IRequestHandler<EditMessageCommand>
{
    public async Task Handle(EditMessageCommand request, CancellationToken cancellationToken)
    {
        var userId = provider.Current.UserId;
        if (!await storage.ExistMessageAsync(request.Id, userId, cancellationToken))
            throw new MessageNotFoundException(request.Id);

        var telegramBot = await storage.GetTelegramBotAsync(request.Id, userId, cancellationToken);
        if (telegramBot is null)
            throw new TelegramNotFoundException();

        var token = cryptoAes.Decrypt(options.SecretKey, telegramBot.ApiTelegram);

        var bot = new TelegramBotClient(token);
        var files = await telegramService.GetFileMessageInTelegramByFile(bot, request.NewFiles, telegramBot.ChatId,
            cancellationToken);

        await storage.UpdateMessageAsync(request, files, cancellationToken);
    }
}