namespace TgPoster.API.Models;

/// <summary>
///     Запрос списка обнаруженных каналов
/// </summary>
public sealed class ListDiscoverRequest : PaginationRequest
{
    /// <summary>
    ///     Фильтр по тематике (Category)
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    ///     Поиск по названию или username
    /// </summary>
    public string? Search { get; init; }

    /// <summary>
    ///     Тип: "channel" (канал) или "chat" (чат). null — без фильтра.
    /// </summary>
    public string? PeerType { get; init; }
}
