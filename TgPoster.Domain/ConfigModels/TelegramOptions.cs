using Security;

namespace TgPoster.Domain.ConfigModels;

public class TelegramOptions
{
    public required string SecretKey { get; set; }
}