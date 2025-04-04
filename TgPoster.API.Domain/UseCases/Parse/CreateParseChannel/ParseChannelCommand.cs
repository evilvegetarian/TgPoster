using MediatR;
using Security.Interfaces;
using Telegram.Bot;
using TgPoster.API.Domain.ConfigModels;
using TgPoster.API.Domain.Exceptions;
using TgPoster.API.Domain.Extensions;

namespace TgPoster.API.Domain.UseCases.Parse.CreateParseChannel;

public sealed record ParseChannelCommand(
    string Channel,
    bool AlwaysCheckNewPosts,
    Guid ScheduleId,
    bool DeleteText,
    bool DeleteMedia,
    string[] AvoidWords,
    bool NeedVerifiedPosts
) : IRequest;

internal class ParseChannelUseCase(IParseChannelStorage storage, ICryptoAES cryptoAes, TelegramOptions telegramOptions)
    : IRequestHandler<ParseChannelCommand>
{
    public async Task Handle(ParseChannelCommand request, CancellationToken cancellationToken)
    {
        var cryptoToken = await storage.GetTelegramToken(request.ScheduleId, cancellationToken);
        if (cryptoToken is null)
        {
            throw new ScheduleNotFoundException();
        }

        var token = cryptoAes.Encrypt(telegramOptions.SecretKey, cryptoToken);
        var bot = new TelegramBotClient(token);
        var channel = request.Channel.ConvertToTelegramHandle();
        var chat = await bot.GetChat(channel, cancellationToken: cancellationToken);

        await storage.AddParseChannelParameters(channel, request.AlwaysCheckNewPosts, request.ScheduleId,
            request.DeleteText, request.DeleteMedia, request.AvoidWords, request.NeedVerifiedPosts, cancellationToken);
    }
}

public interface IParseChannelStorage
{
    Task AddParseChannelParameters(
        string channel,
        bool alwaysCheckNewPosts,
        Guid scheduleId,
        bool deleteText,
        bool deleteMedia,
        string[] avoidWords,
        bool needVerifiedPosts,
        CancellationToken cancellationToken
    );

    Task<string?> GetTelegramToken(Guid scheduleId, CancellationToken cancellationToken);
}