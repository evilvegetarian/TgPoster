using System.ComponentModel.DataAnnotations;
using MediatR;

namespace TgPoster.API.Domain.UseCases.Discover.SimilarChannels;

public sealed record GetSimilarDiscoverQuery(Guid ChannelId) : IRequest<SimilarDiscoverResponse>;

public sealed record SaveSimilarDiscoverCommand(Guid ChannelId, IReadOnlyList<long> TelegramIds) : IRequest<int>;

public sealed record SimilarDiscoverResponse(
	[property: Required] SimilarDiscoverSource Source, [property: Required] IReadOnlyList<SimilarDiscoverChannel> Channels);

public enum SimilarDiscoverSource
{
	Telegram,
	Catalog
}

public sealed record SimilarDiscoverChannel(
	Guid? DiscoverId, long? TelegramId, string? Username, string? Title, string? TgUrl, int? ParticipantsCount);
