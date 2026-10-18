using MediatR;

namespace TgPoster.API.Domain.UseCases.Repost.SetRepostDestinationsActive;

/// <summary>
/// Включить или выключить сразу несколько целевых каналов одних настроек репоста
/// </summary>
/// <param name="SettingsId"></param>
/// <param name="DestinationIds"></param>
/// <param name="IsActive"></param>
public sealed record SetRepostDestinationsActiveCommand(
	Guid SettingsId,
	List<Guid> DestinationIds,
	bool IsActive) : IRequest<SetRepostDestinationsActiveResponse>;
