using MassTransit;
using MediatR;
using Shared.TgStat;

namespace TgPoster.API.Domain.UseCases.TgStat.ScrapeChannels;

internal sealed class ScrapeChannelsUseCase(IBus bus) : IRequestHandler<ScrapeChannelsCommand>
{
	public async Task Handle(ScrapeChannelsCommand request, CancellationToken ct)
	{
		if (request.Urls.Length == 0)
			throw new ArgumentException("Необходимо указать хотя бы один URL");

		foreach (var url in request.Urls)
		{
			await bus.Publish(new ScrapeChannelContract { Url = url }, ct);
		}
	}
}
