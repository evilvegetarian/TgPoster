namespace Shared.Telegram;

/// <summary>
///     Информация о Telegram-сущности, полученная из публичного HTTP-превью t.me/&lt;username&gt;.
///     Для <see cref="TelegramEntityType.User"/> и <see cref="TelegramEntityType.Bot"/> заполняется минимум полей.
///     Для <see cref="TelegramEntityType.NotFound"/> заполнены только <see cref="Username"/> и <see cref="Type"/>.
/// </summary>
public sealed record TelegramPublicEntityInfo
{
    /// <summary>
    ///     Нормализованный username без префикса <c>@</c> и без <c>t.me/</c>.
    ///     Может быть <c>null</c>, если сущность получена со страницы приглашения <c>t.me/+&lt;hash&gt;</c>
    ///     (у приватных каналов/групп публичного username нет).
    /// </summary>
    public string? Username { get; init; }

    /// <summary>Тип сущности.</summary>
    public required TelegramEntityType Type { get; init; }

    /// <summary>Название канала/группы либо отображаемое имя пользователя/бота, если доступно.</summary>
    public string? Title { get; init; }

    /// <summary>Описание канала/группы из <c>tgme_page_description</c>, если есть.</summary>
    public string? Description { get; init; }

    /// <summary>Число подписчиков (для канала) или участников (для группы), если удалось извлечь.</summary>
    public long? MembersCount { get; init; }

    /// <summary>URL аватара из <c>tgme_page_photo_image</c>, если есть.</summary>
    public string? PhotoUrl { get; init; }
}
