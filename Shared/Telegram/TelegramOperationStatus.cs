namespace Shared.Telegram;

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
    UnknownError
}
