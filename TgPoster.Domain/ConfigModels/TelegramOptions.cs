using Security;

namespace TgPoster.Domain.ConfigModels;

public class TelegramOptions : BaseOptions
{
    public override required string SecretKey { get; set; }
}