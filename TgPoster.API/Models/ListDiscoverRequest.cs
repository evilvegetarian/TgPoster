using System.ComponentModel.DataAnnotations;

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

    /// <summary>
    ///     Минимальное число подписчиков (включительно)
    /// </summary>
    [Range(0, int.MaxValue)]
    public int? MinParticipants { get; init; }

    /// <summary>
    ///     Максимальное число подписчиков (включительно)
    /// </summary>
    [Range(0, int.MaxValue)]
    public int? MaxParticipants { get; init; }

    /// <summary>
    ///     Поле сортировки. По умолчанию — по числу подписчиков
    /// </summary>
    public DiscoverSortBy SortBy { get; init; } = DiscoverSortBy.Participants;

    /// <summary>
    ///     Направление сортировки. По умолчанию — по убыванию
    /// </summary>
    public SortDirection SortDirection { get; init; } = SortDirection.Desc;
}

/// <summary>
///     Поле для сортировки обнаруженных каналов
/// </summary>
public enum DiscoverSortBy
{
    /// <summary>
    ///     По количеству подписчиков
    /// </summary>
    Participants,

    /// <summary>
    ///     По дате обнаружения
    /// </summary>
    DiscoveredAt,

    /// <summary>
    ///     По названию
    /// </summary>
    Title
}
