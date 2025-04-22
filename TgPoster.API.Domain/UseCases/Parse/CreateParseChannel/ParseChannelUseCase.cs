using Contracts;
using MassTransit;
using MediatR;
using Security.Interfaces;
using Telegram.Bot;
using TgPoster.API.Domain.ConfigModels;
using TgPoster.API.Domain.Exceptions;
using TgPoster.API.Domain.Extensions;

namespace TgPoster.API.Domain.UseCases.Parse.CreateParseChannel;

internal class ParseChannelUseCase(
    IParseChannelStorage storage,
    ICryptoAES cryptoAes,
    TelegramOptions telegramOptions,
    IBus bus)
    : IRequestHandler<ParseChannelCommand>
{
    public async Task Handle(ParseChannelCommand request, CancellationToken cancellationToken)
    {
        var cryptoToken = await storage.GetTelegramToken(request.ScheduleId, cancellationToken);
        if (cryptoToken is null)
            throw new ScheduleNotFoundException();

        var token = cryptoAes.Decrypt(telegramOptions.SecretKey, cryptoToken);
        var bot = new TelegramBotClient(token);
        var channel = request.Channel.ConvertToTelegramHandle();
        var chat = await bot.GetChat(channel, cancellationToken: cancellationToken);

        var id = await storage.AddParseChannelParameters(channel, request.AlwaysCheckNewPosts, request.ScheduleId,
            request.DeleteText, request.DeleteMedia, request.AvoidWords, request.NeedVerifiedPosts, cancellationToken);
        await bus.Publish(new ParseChannelContract { Id = id }, cancellationToken);
    }
}