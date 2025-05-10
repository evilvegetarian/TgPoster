using Security.Interfaces;
using TgPoster.API.Domain.ConfigModels;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.Services;

public class TelegramTokenService(
    IGetTelegramBotStorage storage,
    IIdentityProvider identity,
    ICryptoAES aes,
    TelegramOptions options
)
{
    public async Task<string> GetTokenByScheduleIdAsync(
        Guid scheduleId,
        CancellationToken ct
    )
    {
        var userId = identity.Current.UserId;
        var encryptedToken = await storage.GetApiTokenAsync(scheduleId, userId, ct);
        if (encryptedToken == null)
        {
            throw new TelegramNotFoundException();
        }

        var token = aes.Decrypt(options.SecretKey, encryptedToken);
        return token;
    }
}