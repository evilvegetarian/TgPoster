using Security;

namespace TgPoster.Domain.ConfigModels;

public class TelegramOptions : BaseOptions
{
    public override string SecretKey { get; set; }
}