using MediatR;
using Security.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgPoster.API.Domain.ConfigModels;
using TgPoster.API.Domain.Exceptions;
using TgPoster.API.Domain.Extensions;

namespace TgPoster.API.Domain.UseCases.Schedules.CreateSchedule;

internal sealed class CreateScheduleUseCase(
    TelegramOptions options,
    ICreateScheduleStorage storage,
    IIdentityProvider identity,
    ICryptoAES aes
) : IRequestHandler<CreateScheduleCommand, CreateScheduleResponse>
{
    public async Task<CreateScheduleResponse> Handle(CreateScheduleCommand request, CancellationToken ct)
    {
        var userId = identity.Current.UserId;
        var encryptedToken = await storage.GetApiTokenAsync(request.TelegramBotId, userId, ct);
        if (encryptedToken is null)
        {
            throw new TelegramNotFoundException();
        }

        var token = aes.Decrypt(options.SecretKey, encryptedToken);

        var bot = new TelegramBotClient(token);
        var userNameChat = request.Channel.ConvertToTelegramHandle();
        var channel = await bot.GetChat(userNameChat, ct);
        var botMember = await bot.GetChatMember(userNameChat, bot.BotId, ct);
        if (botMember is not ChatMemberAdministrator botAdminMember || !botAdminMember.CanPostMessages)
        {
            throw new TelegramBotNotPermission();
        }

        var newSchedule = await storage.CreateScheduleAsync(
            request.Name,
            identity.Current.UserId,
            request.TelegramBotId,
            channel.Id,
            userNameChat,
            ct);
        return new CreateScheduleResponse
        {
            Id = newSchedule
        };
    }
}