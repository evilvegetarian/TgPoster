namespace TgPoster.API.Domain.UseCases.Discover.GetSources;

public interface IGetSourcesStorage
{
	Task<List<DiscoverSourceOption>> GetSourcesAsync(CancellationToken ct);
}
