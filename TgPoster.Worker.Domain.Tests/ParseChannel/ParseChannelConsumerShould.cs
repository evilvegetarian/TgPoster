using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;
using TgPoster.Worker.Domain.Services.ScheduleAlerts;
using TgPoster.Worker.Domain.UseCases.ParseChannel;
using TgPoster.Worker.Domain.UseCases.ParseChannelConsumer;

namespace TgPoster.Worker.Domain.Tests.ParseChannel;

public sealed class ParseChannelConsumerShould
{
	private readonly Mock<IParseChannelConsumerStorage> storage = new();
	private readonly Mock<IParseChannelUseCaseStorage> useCaseStorage = new();
	private readonly Mock<IScheduleAlertService> alertService = new();
	private readonly Guid id = Guid.NewGuid();
	private readonly Guid scheduleId = Guid.NewGuid();
	private readonly ParseChannelConsumer sut;

	public ParseChannelConsumerShould()
	{
		storage.Setup(s => s.GetScheduleIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(scheduleId);

		var useCase = new ParseChannelUseCase(
			useCaseStorage.Object,
			Mock.Of<IPublishEndpoint>(),
			Mock.Of<ITelegramMessageService>(),
			alertService.Object,
			NullLogger<ParseChannelUseCase>.Instance);

		sut = new ParseChannelConsumer(
			NullLogger<ParseChannelConsumer>.Instance,
			storage.Object,
			useCase,
			alertService.Object);
	}

	[Fact]
	public async Task MarkFailedAndNotify_WhenParsingThrows()
	{
		useCaseStorage.Setup(s => s.GetChannelParsingParametersAsync(id, It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("БД недоступна"));

		await sut.Consume(CreateContext(CancellationToken.None));

		storage.Verify(s => s.UpdateErrorStatusAsync(id), Times.Once);
		alertService.Verify(a => a.NotifyAsync(scheduleId, ScheduleProblem.ParsingFailed, "БД недоступна", id,
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task NotNotify_WhenParsingCanceledByShutdown()
	{
		using var cts = new CancellationTokenSource();
		await cts.CancelAsync();
		useCaseStorage.Setup(s => s.GetChannelParsingParametersAsync(id, It.IsAny<CancellationToken>()))
			.ThrowsAsync(new OperationCanceledException(cts.Token));

		await sut.Consume(CreateContext(cts.Token));

		alertService.Verify(a => a.NotifyAsync(It.IsAny<Guid>(), It.IsAny<ScheduleProblem>(), It.IsAny<string>(),
			It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	private ConsumeContext<ParseChannelContract> CreateContext(CancellationToken ct)
	{
		var context = new Mock<ConsumeContext<ParseChannelContract>>();
		context.SetupGet(x => x.Message).Returns(new ParseChannelContract { Id = id });
		context.SetupGet(x => x.CancellationToken).Returns(ct);
		return context.Object;
	}
}
