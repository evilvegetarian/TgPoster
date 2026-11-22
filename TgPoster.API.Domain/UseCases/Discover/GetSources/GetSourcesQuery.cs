using System.ComponentModel.DataAnnotations;
using MediatR;

namespace TgPoster.API.Domain.UseCases.Discover.GetSources;

public sealed record GetSourcesQuery : IRequest<List<DiscoverSourceOption>>;

public sealed record DiscoverSourceOption([property: Required] Guid Id, string? Title, string? Username, string? TgUrl);
