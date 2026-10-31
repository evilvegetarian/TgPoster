using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;
using TgPoster.Worker.Domain.UseCases.ParseChannel;

namespace TgPoster.Storage.Tests.Tests;

public sealed class ProcessMessageConsumerStorageShould(StorageTestFixture fixture)
	: IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly ProcessMessageConsumerStorage sut = new(fixture.GetDbContext(), new GuidFactory());

	[Fact]
	public async Task CreateMessageAsync_ShouldCreateSinglePost_WhenTwoHandlersProcessSameSource()
	{
		var source = await new ChannelParsingSettingBuilder(context).CreateAsync();
		var dto = new MessageDto
		{
			ScheduleId = source.ScheduleId, ChannelParsingSettingId = source.Id, SourceMessageId = 42,
			Text = "Пост источника", TimePosting = DateTimeOffset.UtcNow.AddDays(1)
		};
		await using var firstContext = fixture.GetDbContext();
		await using var secondContext = fixture.GetDbContext();

		await Task.WhenAll(
			new ProcessMessageConsumerStorage(firstContext, new GuidFactory()).CreateMessageAsync(dto, CancellationToken.None),
			new ProcessMessageConsumerStorage(secondContext, new GuidFactory()).CreateMessageAsync(dto, CancellationToken.None));

		await using var check = fixture.GetDbContext();
		(await check.Messages.CountAsync(x => x.ChannelParsingSettingId == source.Id && x.SourceMessageId == 42))
			.ShouldBe(1);
	}

	[Fact]
	public async Task CreateMessageAsync_ShouldNotRestoreDeletedPost_WhenCommandIsDeliveredAgain()
	{
		var source = await new ChannelParsingSettingBuilder(context).CreateAsync();
		var dto = new MessageDto
		{
			ScheduleId = source.ScheduleId, ChannelParsingSettingId = source.Id, SourceMessageId = 43,
			Text = "Удалённая копия", TimePosting = DateTimeOffset.UtcNow.AddDays(1)
		};
		await sut.CreateMessageAsync(dto, CancellationToken.None);
		var created = await context.Messages.SingleAsync(x => x.ChannelParsingSettingId == source.Id && x.SourceMessageId == 43);
		context.Messages.Remove(created);
		await context.SaveChangesAsync();

		await sut.CreateMessageAsync(dto, CancellationToken.None);

		await using var check = fixture.GetDbContext();
		(await check.Messages.CountAsync(x => x.ChannelParsingSettingId == source.Id && x.SourceMessageId == 43)).ShouldBe(0);
		(await check.Messages.IgnoreQueryFilters().CountAsync(x => x.ChannelParsingSettingId == source.Id && x.SourceMessageId == 43))
			.ShouldBe(1);
	}

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

	[Theory]
	[InlineData(true, false)]
	[InlineData(false, true)]
	public async Task CreateMessageAsync_ShouldSaveVerificationFromSource(bool isNeedVerified, bool expectedIsVerified)
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var messageDto = new MessageDto
		{
			Text = Guid.NewGuid().ToString(),
			ScheduleId = schedule.Id,
			IsNeedVerified = isNeedVerified,
			TimePosting = DateTimeOffset.UtcNow.AddHours(1)
		};

		await sut.CreateMessageAsync(messageDto, CancellationToken.None);

		var message = await context.Messages
			.AsNoTracking()
			.SingleAsync(m => m.ScheduleId == schedule.Id);
		message.IsVerified.ShouldBe(expectedIsVerified);
	}

	[Fact]
	public async Task CreateMessageAsync_ShouldSaveSourceMessageId()
	{
		var source = new ChannelParsingSettingBuilder(context).Create();
		var messageDto = new MessageDto
		{
			Text = Guid.NewGuid().ToString(),
			ScheduleId = source.ScheduleId,
			ChannelParsingSettingId = source.Id,
			SourceMessageId = 777,
			TimePosting = DateTimeOffset.UtcNow.AddHours(1)
		};

		await sut.CreateMessageAsync(messageDto, CancellationToken.None);

		var parsed = await sut.IsSourceMessageParsedAsync(source.Id, 777, CancellationToken.None);
		var otherSourceMessage = await sut.IsSourceMessageParsedAsync(source.Id, 778, CancellationToken.None);
		parsed.ShouldBeTrue();
		otherSourceMessage.ShouldBeFalse();
	}
}
