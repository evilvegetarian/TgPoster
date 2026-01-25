using TL;

namespace Shared.Telegram;

/// <summary>
///     Информация о Telegram чате/канале.
/// </summary>
public sealed class TelegramChatInfo
{
    /// <summary>
    ///     ID чата.
    /// </summary>
    public required long Id { get; init; }

    /// <summary>
    ///     Access hash для API вызовов.
    /// </summary>
    public required long AccessHash { get; init; }

    /// <summary>
    ///     Название чата/канала.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    ///     Username (если публичный).
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    ///     Это канал (не группа).
    /// </summary>
    public bool IsChannel { get; init; }

    /// <summary>
    ///     Это группа/супергруппа.
    /// </summary>
    public bool IsGroup { get; init; }

    /// <summary>
    ///     Можно отправлять сообщения.
    /// </summary>
    public bool CanSendMessages { get; init; }

    /// <summary>
    ///     Можно отправлять медиа.
    /// </summary>
    public bool CanSendMedia { get; init; }

    /// <summary>
    ///     Пользователь является администратором.
    /// </summary>
    public bool IsAdmin { get; init; }

    /// <summary>
    ///     Пользователь является создателем.
    /// </summary>
    public bool IsCreator { get; init; }

    /// <summary>
    ///     InputPeer для API вызовов.
    /// </summary>
    public required InputPeer InputPeer { get; init; }
}
