using MediatR;
using TgPoster.API.Domain.UseCases.Repost.GetRepostImportJob;

namespace TgPoster.API.Domain.UseCases.Repost.AddDestinationsFromDiscover;

/// <summary>
///     Массовое добавление целевых каналов репоста из Discover
/// </summary>
/// <param name="RepostSettingsId">Id настроек репоста, куда добавляем каналы</param>
/// <param name="DiscoveredChannelIds">Id обнаруженных каналов из Discover. Игнорируются, если задан Filter</param>
/// <param name="AutoJoin">Вступать в канал, если аккаунт ещё не участник</param>
/// <param name="Filter">Фильтр Discover: каналы отбираются по нему, а не по списку id (null — берём список id)</param>
public sealed record AddDestinationsFromDiscoverCommand(
	Guid RepostSettingsId,
	IReadOnlyList<Guid> DiscoveredChannelIds,
	bool AutoJoin,
	DiscoverImportFilter? Filter = null) : IRequest<RepostImportJobResponse>;
