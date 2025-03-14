using MediatR;
using Microsoft.Extensions.Options;
using Security.Interfaces;
using Telegram.Bot;
using TgPoster.Domain.ConfigModels;
using TgPoster.Domain.Exceptions;
using TgPoster.Domain.Services;

namespace TgPoster.Domain.UseCases.Messages.CreateMessagesFromFiles;

internal sealed class CreateMessagesFromFilesUseCase(
    ICreateMessagesFromFilesUseCaseStorage storage,
    IIdentityProvider identity,
    ICryptoAES cryptoAes,
    TelegramOptions options,
    TelegramService telegramService,
    TimePostingService timePostingService
) : IRequestHandler<CreateMessagesFromFilesCommand>
{
    public async Task Handle(CreateMessagesFromFilesCommand request, CancellationToken cancellationToken)
    {
        var userId = identity.Current.UserId;
        var telegramBot = await storage.GetTelegramBot(request.ScheduleId, userId, cancellationToken);
        if (telegramBot == null)
            throw new TelegramNotFoundException();

        var token = cryptoAes.Decrypt(options.SecretKey, telegramBot.ApiTelegram);
        var bot = new TelegramBotClient(token);
        var files = await telegramService.GetFileMessageInTelegramByFile(
            bot,
            request.Files,
            telegramBot.ChatId,
            cancellationToken);
        var existTime = await storage.GetExistMessageTimePosting(request.ScheduleId, cancellationToken);
        var scheduleTime = await storage.GetScheduleTime(request.ScheduleId, cancellationToken);

        var postingTime = timePostingService.GetTimeForPosting(request.Files.Count, scheduleTime, existTime);

        await storage.CreateMessages(request.ScheduleId, files, postingTime, cancellationToken);
    }
}