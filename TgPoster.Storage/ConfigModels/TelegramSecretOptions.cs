namespace TgPoster.Storage.ConfigModels;

/// <summary>
///     Ключ шифрования токенов Telegram ботов (секция TelegramOptions)
/// </summary>
internal sealed class TelegramSecretOptions
{
	public required string SecretKey { get; init; }
}
