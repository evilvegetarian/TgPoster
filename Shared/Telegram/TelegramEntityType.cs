namespace Shared.Telegram;

/// <summary>
///     Тип Telegram-сущности, определённый по публичному HTTP-превью t.me.
/// </summary>
public enum TelegramEntityType
{
    /// <summary>Username не существует или страница t.me не содержит информации о сущности.</summary>
    NotFound,

    /// <summary>Публичный канал (broadcast).</summary>
    Channel,

    /// <summary>Публичная группа или супергруппа.</summary>
    Group,

    /// <summary>Бот (username оканчивается на <c>bot</c>).</summary>
    Bot,

    /// <summary>Обычный пользователь.</summary>
    User
}
