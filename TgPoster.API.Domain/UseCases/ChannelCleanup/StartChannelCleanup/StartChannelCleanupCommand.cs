using MediatR;
using TgPoster.API.Domain.UseCases.ChannelCleanup.GetChannelCleanupJob;

namespace TgPoster.API.Domain.UseCases.ChannelCleanup.StartChannelCleanup;

/// <summary>
/// Запустить выход из каналов, подтверждённых в предпросмотре
/// </summary>
/// <param name="SessionId"></param>
/// <param name="ChatIds"></param>
public sealed record StartChannelCleanupCommand(Guid SessionId, IReadOnlyList<long> ChatIds)
	: IRequest<ChannelCleanupJobResponse>;
