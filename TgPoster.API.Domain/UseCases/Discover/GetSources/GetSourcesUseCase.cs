using MediatR;

namespace TgPoster.API.Domain.UseCases.Discover.GetSources;

internal sealed class GetSourcesUseCase(IGetSourcesStorage storage)
	: IRequestHandler<GetSourcesQuery, List<DiscoverSourceOption>>
{
	public Task<List<DiscoverSourceOption>> Handle(GetSourcesQuery request, CancellationToken ct) =>
		storage.GetSourcesAsync(ct);
}
