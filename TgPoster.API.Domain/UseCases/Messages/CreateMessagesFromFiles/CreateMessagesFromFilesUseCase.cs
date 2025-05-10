using MediatR;
using Security.Interfaces;
using Shared;
using Telegram.Bot;
using TgPoster.API.Domain.ConfigModels;
using TgPoster.API.Domain.Exceptions;
using TgPoster.API.Domain.Services;

namespace TgPoster.API.Domain.UseCases.Messages.CreateMessagesFromFiles;

internal sealed class CreateMessagesFromFilesUseCase(
    ICreateMessagesFromFilesUseCaseStorage storage,
    IIdentityProvider identity,
    ICryptoAES cryptoAes,
    TelegramOptions options,
    TelegramService telegramService,
    TimePostingService timePostingService
) : IRequestHandler<CreateMessagesFromFilesCommand>
{
    public async Task Handle(CreateMessagesFromFilesCommand request, CancellationToken ct)
    {
        var userId = identity.Current.UserId;
        var telegramBot = await storage.GetTelegramBotAsync(request.ScheduleId, userId, ct);
        if (telegramBot == null)
        {
            throw new TelegramNotFoundException();
        }

        var token = cryptoAes.Decrypt(options.SecretKey, telegramBot.ApiTelegram);
        var bot = new TelegramBotClient(token);
        var files = await telegramService.GetFileMessageInTelegramByFile(
            bot,
            request.Files,
            telegramBot.ChatId,
            ct);
        var existTime = await storage.GetExistMessageTimePostingAsync(request.ScheduleId, ct);
        var scheduleTime = await storage.GetScheduleTimeAsync(request.ScheduleId, ct);

        var postingTime = timePostingService.GetTimeForPosting(request.Files.Count, scheduleTime, existTime);

        await storage.CreateMessagesAsync(request.ScheduleId, files, postingTime, ct);
    }
}