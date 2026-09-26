using MediatR;

namespace TgPoster.API.Domain.UseCases.Discover.GetDiscoverStatus;

internal sealed class GetDiscoverStatusUseCase(IGetDiscoverStatusStorage storage)
	: IRequestHandler<GetDiscoverStatusQuery, DiscoverStatusResponse>
{
	public async Task<DiscoverStatusResponse> Handle(GetDiscoverStatusQuery request, CancellationToken ct)
	{
		var state = await storage.GetDiscoverJobStateAsync(ct);
		return WorkerJobStatusMapper.ToResponse(state, DateTimeOffset.UtcNow);
	}
}
