namespace TgPoster.API.Domain.UseCases.Discover.GetCategories;

public interface IGetCategoriesStorage
{
    Task<List<string>> GetCategoriesAsync(CancellationToken ct);
}
