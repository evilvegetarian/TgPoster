using MediatR;
using TgPoster.API.Domain.UseCases.Discover.GetDiscoverStatus;

namespace TgPoster.API.Domain.UseCases.Parse.GetParseChannelStatus;

internal sealed class GetParseChannelStatusUseCase(IGetParseChannelStatusStorage storage, TimeProvider timeProvider)
	: IRequestHandler<GetParseChannelStatusQuery, DiscoverStatusResponse>
{
	public async Task<DiscoverStatusResponse> Handle(GetParseChannelStatusQuery request, CancellationToken ct)
	{
		var state = await storage.GetParseJobStateAsync(ct);
		return WorkerJobStatusMapper.ToResponse(state, timeProvider.GetUtcNow());
	}
}
