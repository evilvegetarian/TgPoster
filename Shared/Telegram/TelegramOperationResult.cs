namespace Shared.Telegram;

/// <summary>
///     Унифицированный результат операции с Telegram API (без возвращаемого значения).
/// </summary>
public readonly struct TelegramOperationResult
{
    public TelegramOperationStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
    public int? FloodWaitSeconds { get; init; }

    public bool IsSuccess => Status == TelegramOperationStatus.Success;

    public bool IsChannelUnavailable => Status is TelegramOperationStatus.UsernameNotFound
        or TelegramOperationStatus.ChannelBanned;

    public static TelegramOperationResult Success() => new() { Status = TelegramOperationStatus.Success };

    public static TelegramOperationResult Failed(TelegramOperationStatus status, string? error = null,
        int? floodWait = null) =>
        new() { Status = status, ErrorMessage = error, FloodWaitSeconds = floodWait };
}

/// <summary>
///     Унифицированный результат операции с Telegram API с возвращаемым значением.
/// </summary>
public readonly struct TelegramOperationResult<T>
{
    public TelegramOperationStatus Status { get; init; }
    public T? Value { get; init; }
    public string? ErrorMessage { get; init; }
    public int? FloodWaitSeconds { get; init; }

    public bool IsSuccess => Status == TelegramOperationStatus.Success;

    public bool IsChannelUnavailable => Status is TelegramOperationStatus.UsernameNotFound
        or TelegramOperationStatus.ChannelBanned;

    public static TelegramOperationResult<T> Success(T value) =>
        new() { Status = TelegramOperationStatus.Success, Value = value };

    public static TelegramOperationResult<T> Failed(TelegramOperationStatus status, string? error = null,
        int? floodWait = null) =>
        new() { Status = status, ErrorMessage = error, FloodWaitSeconds = floodWait };
}
