using MediatR;
using TgPoster.API.Domain.UseCases.Discover.GetDiscoverStatus;

namespace TgPoster.API.Domain.UseCases.Discover.GetClassificationStatus;

internal sealed class GetClassificationStatusUseCase(IGetClassificationStatusStorage storage)
	: IRequestHandler<GetClassificationStatusQuery, DiscoverStatusResponse>
{
	public async Task<DiscoverStatusResponse> Handle(GetClassificationStatusQuery request, CancellationToken ct)
	{
		var state = await storage.GetClassificationJobStateAsync(ct);
		return WorkerJobStatusMapper.ToResponse(state, DateTimeOffset.UtcNow);
	}
}
