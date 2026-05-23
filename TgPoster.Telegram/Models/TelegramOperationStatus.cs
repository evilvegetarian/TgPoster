namespace TgPoster.Telegram.Models;

/// <summary>
///     Унифицированный статус результата операции с Telegram API.
/// </summary>
public enum TelegramOperationStatus
{
    Success,
    UsernameNotFound,
    ChannelBanned,
    FloodWait,
    AccessDenied,
    Timeout,
    SessionNotFound,
    UnknownError
}
