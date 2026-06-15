namespace TgPoster.API.Domain.UseCases.Repost.AddRepostDestination;

/// <summary>
///     Результат добавления целевого канала репоста.
/// </summary>
/// <param name="DestinationId">Id созданного целевого канала.</param>
/// <param name="DiscoveredChannelId">Id связанной записи в Discover.</param>
public readonly record struct AddDestinationResult(Guid DestinationId, Guid DiscoveredChannelId);
