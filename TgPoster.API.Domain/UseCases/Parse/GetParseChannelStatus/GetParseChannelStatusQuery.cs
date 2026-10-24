using MediatR;
using TgPoster.API.Domain.UseCases.Discover.GetDiscoverStatus;

namespace TgPoster.API.Domain.UseCases.Parse.GetParseChannelStatus;

/// <summary>
/// Запрос состояния фоновой задачи парсинга каналов
/// </summary>
public sealed record GetParseChannelStatusQuery : IRequest<DiscoverStatusResponse>;
