namespace Shared.Telegram;

/// <summary>
///     Сервис определения типа Telegram-сущности по username через публичное HTTP-превью t.me.
///     Не требует MTProto-сессии и подходит для лёгких проверок.
/// </summary>
public interface ITelegramPublicLookupService
{
    /// <summary>
    ///     Делает GET-запрос на <c>https://t.me/&lt;username&gt;</c> и определяет тип сущности.
    /// </summary>
    /// <param name="usernameOrUrl">
    ///     Username в любом формате: <c>name</c>, <c>@name</c>, <c>t.me/name</c>, <c>https://t.me/name</c>.
    /// </param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>
    ///     Результат с <see cref="TelegramPublicEntityInfo"/>. Для несуществующего username возвращается
    ///     <see cref="TelegramOperationStatus.Success"/> с <see cref="TelegramEntityType.NotFound"/>.
    ///     При сетевых сбоях — <see cref="TelegramOperationResult{T}.Failed"/>.
    /// </returns>
    Task<TelegramOperationResult<TelegramPublicEntityInfo>> LookupAsync(
        string usernameOrUrl,
        CancellationToken ct = default);
}
