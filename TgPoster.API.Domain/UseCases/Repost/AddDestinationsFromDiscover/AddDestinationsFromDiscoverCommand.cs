using MediatR;

namespace TgPoster.API.Domain.UseCases.Repost.AddDestinationsFromDiscover;

/// <summary>
///     Массовое добавление целевых каналов репоста из Discover
/// </summary>
/// <param name="RepostSettingsId">Id настроек репоста, куда добавляем каналы</param>
/// <param name="DiscoveredChannelIds">Id обнаруженных каналов из Discover</param>
/// <param name="AutoJoin">Вступать в канал, если аккаунт ещё не участник</param>
public sealed record AddDestinationsFromDiscoverCommand(
	Guid RepostSettingsId,
	IReadOnlyList<Guid> DiscoveredChannelIds,
	bool AutoJoin) : IRequest<AddDestinationsFromDiscoverResponse>;
