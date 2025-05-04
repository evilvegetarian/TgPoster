using MediatR;
using Security.Interfaces;
using Telegram.Bot;
using TgPoster.API.Domain.ConfigModels;
using TgPoster.API.Domain.Exceptions;
using TgPoster.API.Domain.Services;

namespace TgPoster.API.Domain.UseCases.Messages.CreateMessage;

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
        if (!await storage.ExistScheduleAsync(userId, request.ScheduleId, ct))
        {
            throw new ScheduleNotFoundException();
        }

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

        var id = await storage.CreateMessagesAsync(request.ScheduleId, request.Text, request.TimePosting, files, ct);
        return new CreateMessageResponse
        {
            Id = id
        };
    }
}