using MediatR;

namespace TgPoster.API.Domain.UseCases.Repost.DeleteRepostDestinations;

/// <summary>
/// Удалить сразу несколько целевых каналов одних настроек репоста
/// </summary>
/// <param name="SettingsId"></param>
/// <param name="DestinationIds"></param>
public sealed record DeleteRepostDestinationsCommand(Guid SettingsId, List<Guid> DestinationIds) : IRequest;
