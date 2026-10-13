using MediatR;

namespace TgPoster.API.Domain.UseCases.ChannelCleanup.GetChannelCleanupJob;

public sealed record GetChannelCleanupJobQuery(Guid JobId) : IRequest<ChannelCleanupJobResponse>;
