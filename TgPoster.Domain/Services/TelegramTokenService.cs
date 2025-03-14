using Microsoft.Extensions.Options;
using Security.Interfaces;
using TgPoster.Domain.ConfigModels;
using TgPoster.Domain.Exceptions;

namespace TgPoster.Domain.Services;

public class TelegramTokenService(
    IGetTelegramBotStorage storage,
    IIdentityProvider identity,
    ICryptoAES aes,
    TelegramOptions options
)
{
    public async Task<string> GetDecryptToken(Guid scheduleId, CancellationToken cancellationToken)
    {
        var userId = identity.Current.UserId;
        var encryptedToken = await storage.GetApiToken(scheduleId, userId, cancellationToken);
        if (encryptedToken == null)
            throw new TelegramNotFoundException();

        var token = aes.Decrypt(options.SecretKey, encryptedToken);
        return token;
    }
}