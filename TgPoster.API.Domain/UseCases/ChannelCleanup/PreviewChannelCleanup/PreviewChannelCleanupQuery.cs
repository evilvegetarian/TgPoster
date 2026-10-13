using MediatR;

namespace TgPoster.API.Domain.UseCases.ChannelCleanup.PreviewChannelCleanup;

public sealed record PreviewChannelCleanupQuery(Guid SessionId) : IRequest<ChannelCleanupPreviewResponse>;
