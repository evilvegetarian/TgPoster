using Security.Interfaces;
using TgPoster.API.Domain.ConfigModels;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.Services;

public class TelegramTokenService(
    IGetTelegramBotStorage storage,
    IIdentityProvider identity,
    ICryptoAES aes,
    TelegramOptions options)
{
    public async Task<(string token, long chatId)> GetTokenByTelegramIdAsync(Guid id, CancellationToken ct)
    {
        var userId = identity.Current.UserId;
        var telegramBot = await storage.GetApiTokenAsync(id, userId, ct);
        if (telegramBot is null)
            throw new TelegramBotNotFoundException(id);

        var token = aes.Decrypt(options.SecretKey, telegramBot.ApiTelegram);
        return (token, telegramBot.ChatId);
    }

    public async Task<(string token, long chatId)> GetTokenByScheduleIdAsync(Guid scheduleId, CancellationToken ct)
    {
        var userId = identity.Current.UserId;
        var telegramBot = await storage.GetTelegramBotByScheduleIdAsync(scheduleId, userId, ct);
        if (telegramBot is null)
            throw new TelegramBotNotFoundException();

        var token = aes.Decrypt(options.SecretKey, telegramBot.ApiTelegram);
        return (token, telegramBot.ChatId);
    }

    public async Task<(string token, long chatId)> GetTokenByMessageIdAsync(Guid messageId, CancellationToken ct)
    {
        var userId = identity.Current.UserId;
        var telegramBot = await storage.GetTelegramBotByMessageIdAsync(messageId, userId, ct);
        if (telegramBot is null)
            throw new TelegramBotNotFoundException();

        var token = aes.Decrypt(options.SecretKey, telegramBot.ApiTelegram);
        return (token, telegramBot.ChatId);
    }
}