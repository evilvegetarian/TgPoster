namespace TgPoster.API.Domain.UseCases.Discover.GetLanguages;

public interface IGetLanguagesStorage
{
	Task<List<string>> GetLanguagesAsync(CancellationToken ct);
}
