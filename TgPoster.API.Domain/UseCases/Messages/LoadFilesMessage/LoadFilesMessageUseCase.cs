using MediatR;
using Security.Interfaces;
using Telegram.Bot;
using TgPoster.API.Domain.ConfigModels;
using TgPoster.API.Domain.Exceptions;
using TgPoster.API.Domain.Models;
using TgPoster.API.Domain.Services;

namespace TgPoster.API.Domain.UseCases.Messages.LoadFilesMessage;

internal class LoadFilesMessageUseCase(
    ILoadFilesMessageStorage storage,
    TelegramService telegramService,
    ICryptoAES cryptoAes,
    TelegramOptions options,
    IIdentityProvider provider)
    : IRequestHandler<LoadFilesMessageCommand>
{
    public async Task Handle(LoadFilesMessageCommand request, CancellationToken ct)
    {
        var userId = provider.Current.UserId;
        var telegramBot = await storage.GetTelegramBotAsync(request.Id, userId, ct);
        if (telegramBot is null)
            throw new MessageNotFoundException(request.Id);

        var token = cryptoAes.Decrypt(options.SecretKey, telegramBot.ApiTelegram);
        
        var bot = new TelegramBotClient(token);
        var files = await telegramService.GetFileMessageInTelegramByFile(bot, request.Files, telegramBot.ChatId, ct);

        await storage.AddFileAsync(request.Id, files, ct);
    }
}