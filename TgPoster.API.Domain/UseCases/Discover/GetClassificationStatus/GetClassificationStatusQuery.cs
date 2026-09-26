using MediatR;
using TgPoster.API.Domain.UseCases.Discover.GetDiscoverStatus;

namespace TgPoster.API.Domain.UseCases.Discover.GetClassificationStatus;

/// <summary>
///     Запрос состояния фоновой задачи классификации каналов
/// </summary>
public sealed record GetClassificationStatusQuery : IRequest<DiscoverStatusResponse>;
