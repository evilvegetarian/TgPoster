using MediatR;

namespace TgPoster.API.Domain.UseCases.Discover.GetLanguages;

internal sealed class GetLanguagesUseCase(IGetLanguagesStorage storage) : IRequestHandler<GetLanguagesQuery, List<string>>
{
	public Task<List<string>> Handle(GetLanguagesQuery request, CancellationToken ct) =>
		storage.GetLanguagesAsync(ct);
}
