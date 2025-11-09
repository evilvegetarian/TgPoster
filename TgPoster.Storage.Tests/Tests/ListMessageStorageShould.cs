using Shouldly;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;
using SortDirection = TgPoster.API.Domain.UseCases.Messages.ListMessage.SortDirection;

namespace TgPoster.Storage.Tests.Tests;

public sealed class ListMessageStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private static readonly CancellationToken Ct = CancellationToken.None;
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly Helper helper = new(fixture.GetDbContext());
	private readonly ListMessageStorage sut = new(fixture.GetDbContext());

	private static ListMessageQuery CreateQuery(
		Guid scheduleId,
		MessageStatus status = MessageStatus.All,
		MessageSortBy sortBy = MessageSortBy.SentAt,
		SortDirection sortDirection = SortDirection.Asc,
		int pageNumber = 1,
		int pageSize = 10,
		string? searchText = null
	) =>
		new(scheduleId, pageNumber, pageSize, sortBy, sortDirection, searchText, status);

	[Fact]
	public async Task ExistScheduleAsync_WithExistingSchedule_ShouldReturnTrue()
	{
		var schedule = await helper.CreateScheduleAsync();
		var result = await sut.ExistScheduleAsync(schedule.Id, schedule.UserId, Ct);
		result.ShouldBeTrue();
	}

	[Fact]
	public async Task ExistScheduleAsync_WithScheduleForAnotherUser_ShouldReturnFalse()
	{
		var schedule = await helper.CreateScheduleAsync();
		var result = await sut.ExistScheduleAsync(schedule.Id, Guid.NewGuid(), Ct);
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task ExistScheduleAsync_WithNonExistingSchedule_ShouldReturnFalse()
	{
		var result = await sut.ExistScheduleAsync(Guid.NewGuid(), Guid.NewGuid(), Ct);
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task GetApiTokenAsync_WithExistingSchedule_ShouldReturnApiToken()
	{
		var schedule = await helper.CreateScheduleAsync();
		var result = await sut.GetApiTokenAsync(schedule.Id, Ct);
		result.ShouldNotBeNull();
		result.ShouldNotBeEmpty();
	}

	[Fact]
	public async Task GetApiTokenAsync_WithNonExistingSchedule_ShouldReturnNull()
	{
		var result = await sut.GetApiTokenAsync(Guid.NewGuid(), Ct);
		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetMessagesAsync_WithExistingMessages_ShouldReturnMessages()
	{
		var schedule = await helper.CreateScheduleAsync();
		var message1 = await helper.CreateMessageAsync(schedule.Id);
		var message2 = await helper.CreateMessageAsync(schedule.Id);
		await helper.CreateMessageFileAsync(message1.Id);
		await helper.CreateVideoMessageFileAsync(message2.Id);
		var request = CreateQuery(schedule.Id, sortBy: MessageSortBy.SentAt);

		var result = await sut.GetMessagesAsync(request, Ct);

		result.TotalCount.ShouldBe(2);
		result.Items.Count.ShouldBe(2);
		result.Items.ShouldContain(x => x.Id == message1.Id);
		result.Items.ShouldContain(x => x.Id == message2.Id);
		result.Items.All(x => x.ScheduleId == schedule.Id).ShouldBeTrue();
	}

	[Fact]
	public async Task GetMessagesAsync_WithScheduleWithoutMessages_ShouldReturnEmptyList()
	{
		var schedule = await helper.CreateScheduleAsync();
		var request = CreateQuery(schedule.Id);
		var result = await sut.GetMessagesAsync(request, Ct);
		result.TotalCount.ShouldBe(0);
		result.Items.ShouldBeEmpty();
	}

	[Fact]
	public async Task GetMessagesAsync_WithNonExistingSchedule_ShouldReturnEmptyList()
	{
		var request = CreateQuery(Guid.NewGuid());
		var result = await sut.GetMessagesAsync(request, Ct);
		result.Items.ShouldBeEmpty();
		result.TotalCount.ShouldBe(0);
	}

	[Fact]
	public async Task GetMessagesAsync_WithFiles_ShouldReturnMessagesWithFiles()
	{
		var schedule = await helper.CreateScheduleAsync();
		var message = await helper.CreateMessageAsync(schedule.Id);
		var imageFile = await helper.CreateMessageFileAsync(message.Id);
		var videoFile = await helper.CreateVideoMessageFileAsync(message.Id);
		var request = CreateQuery(schedule.Id);

		var result = await sut.GetMessagesAsync(request, Ct);

		result.Items.ShouldNotBeEmpty();
		var returnedMessage = result.Items.First();
		returnedMessage.Files.Count.ShouldBe(2);

		var imageDto = returnedMessage.Files.First(x => x.Id == imageFile.Id);
		imageDto.ContentType.ShouldBe("image/jpeg");
		imageDto.PreviewIds.ShouldBeEmpty();

		var videoDto = returnedMessage.Files.First(x => x.Id == videoFile.Id);
		videoDto.ContentType.ShouldBe("video/mp4");
		videoDto.PreviewIds.ShouldNotBeEmpty();
		videoDto.PreviewIds.Count.ShouldBe(2);
	}

	[Fact]
	public async Task GetMessagesAsync_ShouldReturnCorrectMessageData()
	{
		var schedule = await helper.CreateScheduleAsync();
		var message = await helper.CreateMessageAsync(schedule.Id);
		var request = CreateQuery(schedule.Id);

		var result = await sut.GetMessagesAsync(request, Ct);

		var returnedMessage = result.Items.ShouldHaveSingleItem();
		returnedMessage.Id.ShouldBe(message.Id);
		returnedMessage.TextMessage.ShouldBe(message.TextMessage);
		returnedMessage.ScheduleId.ShouldBe(message.ScheduleId);
		returnedMessage.IsSent.ShouldBeFalse();
	}

	[Fact]
	public async Task GetMessagesAsync_WithSearchText_ShouldReturnOnlyMatchingMessages()
	{
		var schedule = await helper.CreateScheduleAsync();
		var matching = await helper.CreateMessageAsync(schedule.Id);
		matching.TextMessage = "Hello World";
		var nonMatching = await helper.CreateMessageAsync(schedule.Id);
		nonMatching.TextMessage = "Something Else";
		var nullText = await helper.CreateMessageAsync(schedule.Id);
		nullText.TextMessage = null;
		await context.SaveChangesAsync();

		var request = CreateQuery(schedule.Id, searchText: "hello");

		var result = await sut.GetMessagesAsync(request, Ct);

		result.TotalCount.ShouldBe(1);
		result.Items.ShouldHaveSingleItem().Id.ShouldBe(matching.Id);
	}

	[Fact]
	public async Task GetMessagesAsync_WithPagination_ShouldReturnExpectedPageAndTotalCount()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var messages = new List<Message>
		{
			await new MessageBuilder(context).WithSchedule(schedule).CreateAsync(),
			await new MessageBuilder(context).WithSchedule(schedule).CreateAsync(),
			await new MessageBuilder(context).WithSchedule(schedule).CreateAsync(),
			await new MessageBuilder(context).WithSchedule(schedule).CreateAsync(),
			await new MessageBuilder(context).WithSchedule(schedule).CreateAsync(),
		};

		var request = CreateQuery(
			schedule.Id,
			sortBy: MessageSortBy.CreatedAt,
			sortDirection: SortDirection.Asc,
			pageNumber: 2,
			pageSize: 2);

		var result = await sut.GetMessagesAsync(request, Ct);

		result.TotalCount.ShouldBe(5);
		result.Items.Count.ShouldBe(2);
		var expectedIds = messages.Skip(2).Take(2).Select(m => m.Id);
		result.Items.Select(m => m.Id).ShouldBe(expectedIds);
	}

	[Theory]
	[InlineData(SortDirection.Asc)]
	[InlineData(SortDirection.Desc)]
	public async Task GetMessagesAsync_SortBySentAt_ShouldRespectDirection(SortDirection direction)
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();

		var earlier = await new MessageBuilder(context).WithSchedule(schedule)
			.WithTimeMessage(DateTime.UtcNow.AddDays(1)).CreateAsync();
		var later = await new MessageBuilder(context).WithSchedule(schedule).WithTimeMessage(DateTime.UtcNow.AddDays(5))
			.CreateAsync();

		var request = CreateQuery(schedule.Id, sortBy: MessageSortBy.SentAt, sortDirection: direction);

		var result = await sut.GetMessagesAsync(request, Ct);

		var expectedOrder = direction == SortDirection.Asc
			? new[] { earlier.Id, later.Id }
			: new[] { later.Id, earlier.Id };

		result.Items.Select(m => m.Id).ShouldBe(expectedOrder);
	}

	[Fact]
	public async Task GetMessagesAsync_SortByCreatedAtAsc_ShouldReturnSortedMessages()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var messages = new List<Message>
		{
			await new MessageBuilder(context).WithSchedule(schedule).CreateAsync(),
			await new MessageBuilder(context).WithSchedule(schedule).CreateAsync(),
			await new MessageBuilder(context).WithSchedule(schedule).CreateAsync(),
			await new MessageBuilder(context).WithSchedule(schedule).CreateAsync(),
		};
		var request = CreateQuery(schedule.Id, sortBy: MessageSortBy.CreatedAt, sortDirection: SortDirection.Asc);

		var result = await sut.GetMessagesAsync(request, Ct);

		result.Items.ShouldNotBeEmpty();
		result.Items.Select(m => m.Id).ShouldBe(messages.Select(m => m.Id));
		result.Items.Select(m => m.Created).ShouldBeInOrder(Shouldly.SortDirection.Ascending);
	}

	[Fact]
	public async Task GetMessagesAsync_SortByCreatedAtDesc_ShouldReturnSortedMessages()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var message1 = await new MessageBuilder(context).WithSchedule(schedule).CreateAsync();
		var message2 = await new MessageBuilder(context).WithSchedule(schedule).CreateAsync();
		var message3 = await new MessageBuilder(context).WithSchedule(schedule).CreateAsync();
		var message4 = await new MessageBuilder(context).WithSchedule(schedule).CreateAsync();
		var messages = new List<Message> { message4, message3, message2, message1 };
		var request = CreateQuery(schedule.Id, sortBy: MessageSortBy.CreatedAt, sortDirection: SortDirection.Desc);

		var result = await sut.GetMessagesAsync(request, Ct);

		result.Items.ShouldNotBeEmpty();
		result.Items.Select(m => m.Id).ShouldBe(messages.Select(m => m.Id));
		result.Items.Select(m => m.Created).ShouldBeInOrder(Shouldly.SortDirection.Descending);
	}

	[Fact]
	public async Task GetMessagesAsync_FilteredByMessageStatusDelivered_ShouldReturnFilteredMessages()
	{
		var notSendStatus = Enum.GetValues<TgPoster.Storage.Data.Enum.MessageStatus>()
			.First(status => status != Data.Enum.MessageStatus.Send);
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var sendMessage = await new MessageBuilder(context).WithSchedule(schedule)
			.WithStatus(Data.Enum.MessageStatus.Send).CreateAsync();
		var notSendMessage = await new MessageBuilder(context).WithSchedule(schedule).WithStatus(notSendStatus)
			.CreateAsync();

		var request = CreateQuery(schedule.Id, status: MessageStatus.Delivered, sortBy: MessageSortBy.CreatedAt,
			sortDirection: SortDirection.Desc);

		var result = await sut.GetMessagesAsync(request, Ct);

		result.Items.ShouldNotBeEmpty();
		result.Items.Select(x => x.Id).ShouldContain(sendMessage.Id);
		result.Items.ShouldAllBe(x => x.IsSent);
	}

	[Fact]
	public async Task GetMessagesAsync_FilteredByMessageStatusNotApproved_ShouldReturnFilteredMessages()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var messages = new List<Message>
		{
			await new MessageBuilder(context).WithSchedule(schedule).WithIsVerified(false).CreateAsync(),
			await new MessageBuilder(context).WithSchedule(schedule).WithIsVerified(true).CreateAsync(),
			await new MessageBuilder(context).WithSchedule(schedule).WithIsVerified(false).CreateAsync(),
		};

		var request = CreateQuery(schedule.Id, status: MessageStatus.NotApproved, sortBy: MessageSortBy.CreatedAt,
			sortDirection: SortDirection.Desc);

		var result = await sut.GetMessagesAsync(request, Ct);

		result.Items.ShouldNotBeEmpty();
		var expectedIds = messages
			.Where(m => !m.IsVerified)
			.OrderByDescending(m => m.Created)
			.Select(m => m.Id);
		result.Items.Select(m => m.Id).ShouldBe(expectedIds);
		result.Items.ShouldAllBe(x => !x.IsVerified);
	}

	[Fact]
	public async Task GetMessagesAsync_FilteredByMessageStatusPlaned_ShouldReturnFilteredMessages()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();

		var messages = new List<Message>
		{
			await new MessageBuilder(context).WithSchedule(schedule).WithIsVerified(true).CreateAsync(),
			await new MessageBuilder(context).WithSchedule(schedule).WithIsVerified(false).CreateAsync(),
			await new MessageBuilder(context).WithSchedule(schedule).WithIsVerified(true).CreateAsync(),
		};

		var request = CreateQuery(schedule.Id, status: MessageStatus.Planed, sortBy: MessageSortBy.CreatedAt,
			sortDirection: SortDirection.Desc);

		var result = await sut.GetMessagesAsync(request, Ct);

		result.Items.ShouldNotBeEmpty();
		var expectedIds = messages
			.Where(m => m.IsVerified)
			.OrderByDescending(m => m.Created)
			.Select(m => m.Id);
		result.Items.Select(m => m.Id).ShouldBe(expectedIds);
		result.Items.ShouldAllBe(x => x.IsVerified);
	}
}