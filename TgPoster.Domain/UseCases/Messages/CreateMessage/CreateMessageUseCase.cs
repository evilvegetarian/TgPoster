using MediatR;
using Security.Interfaces;
using Telegram.Bot;
using TgPoster.Domain.ConfigModels;
using TgPoster.Domain.Exceptions;
using TgPoster.Domain.Services;

namespace TgPoster.Domain.UseCases.Messages.CreateMessage;

internal sealed class CreateMessageUseCase(
    ICreateMessageStorage storage,
    IIdentityProvider identityProvider,
    ICryptoAES cryptoAes,
    TelegramOptions options,
    TelegramService telegramService
) : IRequestHandler<CreateMessageCommand, CreateMessageResponse>
{
    public async Task<CreateMessageResponse> Handle(CreateMessageCommand request, CancellationToken ct)
    {
        var userId = identityProvider.Current.UserId;
        if (!await storage.ExistSchedule(userId, request.ScheduleId, ct))
            throw new ScheduleNotFoundException();

        var telegramBot = await storage.GetTelegramBot(request.ScheduleId, userId, ct);
        if (telegramBot == null)
            throw new TelegramNotFoundException();

        var token = cryptoAes.Decrypt(options.SecretKey, telegramBot.ApiTelegram);
        var bot = new TelegramBotClient(token);
        var files = await telegramService.GetFileMessageInTelegramByFile(
            bot,
            request.Files,
            telegramBot.ChatId,
            ct);

        var id = await storage.CreateMessages(request.ScheduleId, request.Text, request.TimePosting, files, ct);
        return new CreateMessageResponse
        {
            Id = id
        };
    }
}