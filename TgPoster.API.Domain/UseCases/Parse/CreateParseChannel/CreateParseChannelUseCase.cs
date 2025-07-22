using MassTransit;
using MediatR;
using Security.Interfaces;
using Shared.Contracts;
using Telegram.Bot;
using TgPoster.API.Domain.ConfigModels;
using TgPoster.API.Domain.Exceptions;
using TgPoster.API.Domain.Extensions;

namespace TgPoster.API.Domain.UseCases.Parse.CreateParseChannel;

internal class CreateParseChannelUseCase(
    IParseChannelStorage storage,
    ICryptoAES cryptoAes,
    IIdentityProvider provider,
    TelegramOptions telegramOptions,
    IBus bus)
    : IRequestHandler<CreateParseChannelCommand, CreateParseChannelResponse>
{
    public async Task<CreateParseChannelResponse> Handle(CreateParseChannelCommand request, CancellationToken ct)
    {
        var userId = provider.Current.UserId;
        var cryptoToken = await storage.GetTelegramTokenAsync(request.ScheduleId, userId, ct);
        if (cryptoToken is null)
        {
            throw new ScheduleNotFoundException(request.ScheduleId);
        }

        var token = cryptoAes.Decrypt(telegramOptions.SecretKey, cryptoToken);
        var bot = new TelegramBotClient(token);
        var channel = request.Channel.ConvertToTelegramHandle();
        var chat = await bot.GetChat(channel, ct);

        var id = await storage.AddParseChannelParametersAsync(chat.Username!, request.AlwaysCheckNewPosts,
            request.ScheduleId, request.DeleteText, request.DeleteMedia, request.AvoidWords, request.NeedVerifiedPosts,
            request.DateFrom, request.DateTo, ct);
        await bus.Publish(new ParseChannelContract { Id = id }, ct);

        return new CreateParseChannelResponse
        {
            Id = id
        };
    }
}