using MediatR;
using Microsoft.Extensions.Options;
using Security.Interfaces;
using Telegram.Bot;
using TgPoster.Domain.ConfigModels;
using TgPoster.Domain.Exceptions;

namespace TgPoster.Domain.UseCases.TelegramBots.CreateTelegramBot;

internal class CreateTelegramBotUseCase(
    ICreateTelegramBotStorage storage,
    IIdentityProvider identity,
    ICryptoAES cryptoAes,
    IOptions<TelegramOptions> telegramOptions
) : IRequestHandler<CreateTelegramBotCommand, CreateTelegramBotResponse>
{
    public async Task<CreateTelegramBotResponse> Handle(
        CreateTelegramBotCommand request,
        CancellationToken cancellationToken
    )
    {
        var bot = new TelegramBotClient(request.ApiToken);
        var updates = await bot.GetUpdates(cancellationToken: cancellationToken);
        var chatId = updates
                         .Select(u => u.Message?.Chat.Id)
                         .FirstOrDefault()
                     ?? throw new ChatIdNotFoundException();

        var botInfo = await bot.GetMe(cancellationToken);
        var tokenEncrypted = cryptoAes.Encrypt(telegramOptions, request.ApiToken);
        var id = await storage.CreateTelegramBotAsync(
            tokenEncrypted,
            chatId,
            identity.Current.UserId,
            botInfo.Username!,
            cancellationToken);

        return new CreateTelegramBotResponse
        {
            Id = id
        };
    }
}