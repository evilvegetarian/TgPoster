using MediatR;

namespace TgPoster.API.Domain.UseCases.Repost.PurgeRepostDestinations;

/// <summary>
/// Удалить каналы насовсем: убрать и из общего списка, и из списка банов и удалённых
/// </summary>
/// <param name="SettingsId"></param>
/// <param name="DestinationIds"></param>
public sealed record PurgeRepostDestinationsCommand(Guid SettingsId, List<Guid> DestinationIds) : IRequest;
