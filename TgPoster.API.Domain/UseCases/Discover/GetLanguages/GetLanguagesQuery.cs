using MediatR;

namespace TgPoster.API.Domain.UseCases.Discover.GetLanguages;

public sealed record GetLanguagesQuery : IRequest<List<string>>;
