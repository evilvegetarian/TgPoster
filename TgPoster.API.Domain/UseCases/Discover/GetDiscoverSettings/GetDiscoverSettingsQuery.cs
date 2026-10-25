using MediatR;

namespace TgPoster.API.Domain.UseCases.Discover.GetDiscoverSettings;

/// <summary>
/// Запрос настроек поиска каналов (Discover)
/// </summary>
public sealed record GetDiscoverSettingsQuery : IRequest<DiscoverSettingsResponse>;
