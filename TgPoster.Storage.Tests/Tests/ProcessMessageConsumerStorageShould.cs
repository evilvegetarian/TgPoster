using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class ProcessMessageConsumerStorageShould(StorageTestFixture fixture)
	: IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly ProcessMessageConsumerStorage sut = new(fixture.GetDbContext(), new GuidFactory());

	[Fact]
	public async Task GeLastMessageTimePostingAsync_ShouldIgnoreCancelledAndFailedMessages()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var lastPlanned = DateTimeOffset.UtcNow.AddHours(2);
		await new MessageBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithStatus(MessageStatus.Register)
			.WithTimeMessage(lastPlanned)
			.CreateAsync();
		await new MessageBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithStatus(MessageStatus.Cancel)
			.WithTimeMessage(DateTimeOffset.UtcNow.AddHours(5))
			.CreateAsync();
		await new MessageBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithStatus(MessageStatus.Error)
			.WithTimeMessage(DateTimeOffset.UtcNow.AddHours(6))
			.CreateAsync();

		var result = await sut.GeLastMessageTimePostingAsync(schedule.Id, CancellationToken.None);

		result.ShouldBe(lastPlanned, TimeSpan.FromMilliseconds(1));
	}

	[Fact]
	public async Task GeLastMessageTimePostingAsync_ShouldReturnDefault_WhenOnlyCancelledAndFailedMessages()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		await new MessageBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithStatus(MessageStatus.Cancel)
			.WithTimeMessage(DateTimeOffset.UtcNow.AddHours(5))
			.CreateAsync();
		await new MessageBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithStatus(MessageStatus.Error)
			.WithTimeMessage(DateTimeOffset.UtcNow.AddHours(6))
			.CreateAsync();

		var result = await sut.GeLastMessageTimePostingAsync(schedule.Id, CancellationToken.None);

		result.ShouldBe(default);
	}
}
