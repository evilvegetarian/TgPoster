using MediatR;

namespace TgPoster.API.Domain.UseCases.Repost.RestoreRepostDestination;

/// <summary>
/// Вернуть удалённый канал в общий список. Возвращается выключенным: включение заново проверит доступ
/// </summary>
/// <param name="SettingsId"></param>
/// <param name="DestinationId"></param>
public sealed record RestoreRepostDestinationCommand(Guid SettingsId, Guid DestinationId) : IRequest;
