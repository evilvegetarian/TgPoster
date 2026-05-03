using MediatR;

namespace TgPoster.API.Domain.UseCases.Discover.GetCategories;

internal sealed class GetCategoriesUseCase(IGetCategoriesStorage storage)
    : IRequestHandler<GetCategoriesQuery, List<string>>
{
    public Task<List<string>> Handle(GetCategoriesQuery request, CancellationToken ct) =>
        storage.GetCategoriesAsync(ct);
}
