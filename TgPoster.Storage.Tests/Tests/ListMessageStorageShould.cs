using Shouldly;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;
using TgPoster.Storage.Storages;
using SortDirection = TgPoster.API.Domain.UseCases.Messages.ListMessage.SortDirection;

namespace TgPoster.Storage.Tests.Tests;

public class ListMessageStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
    private readonly Helper helper = new(fixture.GetDbContext());
    private readonly ListMessageStorage sut = new(fixture.GetDbContext());

    [Fact]
    public async Task ExistScheduleAsync_WithExistingSchedule_ShouldReturnTrue()
    {
        var schedule = await helper.CreateScheduleAsync();

        var result = await sut.ExistScheduleAsync(schedule.Id, schedule.UserId, CancellationToken.None);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistScheduleAsync_WithNonExistingSchedule_ShouldReturnFalse()
    {
        var nonExistingScheduleId = Guid.NewGuid();

        var result = await sut.ExistScheduleAsync(nonExistingScheduleId, Guid.NewGuid(), CancellationToken.None);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task GetApiTokenAsync_WithExistingSchedule_ShouldReturnApiToken()
    {
        var schedule = await helper.CreateScheduleAsync();

        var result = await sut.GetApiTokenAsync(schedule.Id, CancellationToken.None);

        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GetApiTokenAsync_WithNonExistingSchedule_ShouldReturnNull()
    {
        var nonExistingScheduleId = Guid.NewGuid();

        var result = await sut.GetApiTokenAsync(nonExistingScheduleId, CancellationToken.None);

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
        var request = new ListMessageQuery(schedule.Id, 1, 10, MessageSortBy.SentAt, SortDirection.Asc, null,
            MessageStatus.All);

        var result = await sut.GetMessagesAsync(request, CancellationToken.None);

        result.Items.ShouldNotBeEmpty();
        result.Items.Count.ShouldBe(2);
        result.Items.ShouldContain(x => x.Id == message1.Id);
        result.Items.ShouldContain(x => x.Id == message2.Id);
        result.Items.All(x => x.ScheduleId == schedule.Id).ShouldBeTrue();
    }

    [Fact]
    public async Task GetMessagesAsync_WithScheduleWithoutMessages_ShouldReturnEmptyList()
    {
        var schedule = await helper.CreateScheduleAsync();
        var request = new ListMessageQuery(schedule.Id, 1, 10, MessageSortBy.SentAt, SortDirection.Asc, null,
            MessageStatus.All);

        var result = await sut.GetMessagesAsync(request, CancellationToken.None);

        result.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetMessagesAsync_WithNonExistingSchedule_ShouldReturnEmptyList()
    {
        var nonExistingScheduleId = Guid.NewGuid();

        var request = new ListMessageQuery(nonExistingScheduleId, 1, 10, MessageSortBy.SentAt, SortDirection.Asc, null,
            MessageStatus.All);

        var result = await sut.GetMessagesAsync(request, CancellationToken.None);

        result.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetMessagesAsync_ShouldReturnMessagesWithFiles()
    {
        var schedule = await helper.CreateScheduleAsync();
        var message = await helper.CreateMessageAsync(schedule.Id);
        var imageFile = await helper.CreateMessageFileAsync(message.Id);
        var videoFile = await helper.CreateVideoMessageFileAsync(message.Id);
        var request = new ListMessageQuery(schedule.Id, 1, 10, MessageSortBy.SentAt, SortDirection.Asc, null,
            MessageStatus.All);

        var result = await sut.GetMessagesAsync(request, CancellationToken.None);

        result.Items.ShouldNotBeEmpty();
        var returnedMessage = result.Items.First();
        returnedMessage.Files.Count.ShouldBe(2);
        returnedMessage.Files.ShouldContain(x => x.Id == imageFile.Id && x.ContentType == "image/jpeg");
        returnedMessage.Files.ShouldContain(x => x.Id == videoFile.Id && x.ContentType == "video/mp4");

        var videoFileDto = returnedMessage.Files.First(x => x.ContentType == "video/mp4");
        videoFileDto.PreviewIds.ShouldNotBeEmpty();
        videoFileDto.PreviewIds.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetMessagesAsync_ShouldReturnCorrectMessageData()
    {
        var schedule = await helper.CreateScheduleAsync();
        var message = await helper.CreateMessageAsync(schedule.Id);
        var request = new ListMessageQuery(schedule.Id, 1, 10, MessageSortBy.SentAt, SortDirection.Asc, null,
            MessageStatus.All);

        var result = await sut.GetMessagesAsync(request, CancellationToken.None);

        result.Items.ShouldNotBeEmpty();
        var returnedMessage = result.Items.First();
        returnedMessage.Id.ShouldBe(message.Id);
        returnedMessage.TextMessage.ShouldBe(message.TextMessage);
        returnedMessage.ScheduleId.ShouldBe(message.ScheduleId);
    }
}