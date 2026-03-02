using MediatR;

namespace TgPoster.API.Domain.UseCases.TgStat.ScrapeChannels;

public sealed record ScrapeChannelsCommand(string[] Urls) : IRequest;
