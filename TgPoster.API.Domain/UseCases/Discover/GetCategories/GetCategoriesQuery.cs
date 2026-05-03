using MediatR;

namespace TgPoster.API.Domain.UseCases.Discover.GetCategories;

public sealed record GetCategoriesQuery : IRequest<List<string>>;
