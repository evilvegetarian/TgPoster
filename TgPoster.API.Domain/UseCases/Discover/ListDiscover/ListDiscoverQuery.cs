using MediatR;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;

namespace TgPoster.API.Domain.UseCases.Discover.ListDiscover;

public sealed record ListDiscoverQuery(
    int Page,
    int PageSize,
    string? Category,
    string? Search,
    string? PeerType,
    int? MinParticipants,
    int? MaxParticipants,
    DiscoverSortBy SortBy,
    SortDirection SortDirection) : IRequest<PagedResponse<DiscoverChannelResponse>>;

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

/// <summary>
///     Направление сортировки
/// </summary>
public enum SortDirection
{
    /// <summary>
    ///     По возрастанию
    /// </summary>
    Asc,

    /// <summary>
    ///     По убыванию
    /// </summary>
    Desc
}
