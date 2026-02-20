using MediatR;

namespace TgPoster.API.Domain.UseCases.Repost.RefreshDestinationInfo;

/// <summary>
///     Команда для обновления информации о целевом канале из Telegram.
/// </summary>
public sealed record RefreshDestinationInfoCommand(Guid DestinationId) : IRequest;
