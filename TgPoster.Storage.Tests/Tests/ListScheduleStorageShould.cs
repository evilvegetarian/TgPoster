using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;

namespace TgPoster.Storage.Tests.Tests;

public class ListScheduleStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
    private readonly PosterContext context = fixture.GetDbContext();
    private readonly Helper helper = new(fixture.GetDbContext());
    private readonly ListScheduleStorage sut = new(fixture.GetDbContext());

    [Fact]
    public async Task GetListScheduleAsync_WithExistingSchedules_ShouldReturnSchedules()
    {
        var user = await helper.CreateUserAsync();
        var telegramBot1 = await helper.CreateTelegramBotAsync(user.Id);
        var telegramBot2 = await helper.CreateTelegramBotAsync(user.Id);
        
        var schedule1 = await CreateScheduleForUser(user.Id, telegramBot1.Id, "Schedule 1");
        var schedule2 = await CreateScheduleForUser(user.Id, telegramBot2.Id, "Schedule 2");

        var result = await sut.GetListScheduleAsync(user.Id, CancellationToken.None);

        result.ShouldNotBeEmpty();
        result.Count.ShouldBe(2);
        result.ShouldContain(x => x.Id == schedule1.Id && x.Name == "Schedule 1");
        result.ShouldContain(x => x.Id == schedule2.Id && x.Name == "Schedule 2");
        result.All(x => x.ChannelName != null).ShouldBeTrue();
    }

    [Fact]
    public async Task GetListScheduleAsync_WithNoSchedules_ShouldReturnEmptyList()
    {
        var user = await helper.CreateUserAsync();

        var result = await sut.GetListScheduleAsync(user.Id, CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetListScheduleAsync_WithNonExistingUser_ShouldReturnEmptyList()
    {
        var nonExistingUserId = Guid.NewGuid();

        var result = await sut.GetListScheduleAsync(nonExistingUserId, CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetListScheduleAsync_ShouldReturnOnlyUserSchedules()
    {
        var user1 = await helper.CreateUserAsync();
        var user2 = await helper.CreateUserAsync();
        
        var telegramBot1 = await helper.CreateTelegramBotAsync(user1.Id);
        var telegramBot2 = await helper.CreateTelegramBotAsync(user2.Id);
        
        var schedule1 = await CreateScheduleForUser(user1.Id, telegramBot1.Id, "User1 Schedule");
        var schedule2 = await CreateScheduleForUser(user2.Id, telegramBot2.Id, "User2 Schedule");

        var result = await sut.GetListScheduleAsync(user1.Id, CancellationToken.None);

        result.ShouldNotBeEmpty();
        result.Count.ShouldBe(1);
        result.First().Id.ShouldBe(schedule1.Id);
        result.First().Name.ShouldBe("User1 Schedule");
    }

    [Fact]
    public async Task GetListScheduleAsync_ShouldReturnCorrectScheduleData()
    {
        var user = await helper.CreateUserAsync();
        var telegramBot = await helper.CreateTelegramBotAsync(user.Id);
        var schedule = await CreateScheduleForUser(user.Id, telegramBot.Id, "Test Schedule", true);

        var result = await sut.GetListScheduleAsync(user.Id, CancellationToken.None);

        result.ShouldNotBeEmpty();
        var returnedSchedule = result.First();
        returnedSchedule.Id.ShouldBe(schedule.Id);
        returnedSchedule.Name.ShouldBe("Test Schedule");
        returnedSchedule.ChannelName.ShouldBe(telegramBot.Name);
        returnedSchedule.IsActive.ShouldBe(true);
    }

    private async Task<TgPoster.Storage.Data.Entities.Schedule> CreateScheduleForUser(
        Guid userId, 
        Guid telegramBotId, 
        string name, 
        bool isActive = false)
    {
        var schedule = new TgPoster.Storage.Data.Entities.Schedule
        {
            Id = Guid.NewGuid(),
            Name = name,
            UserId = userId,
            TelegramBotId = telegramBotId,
            ChannelId = -1001234567890L,
            ChannelName = "Test Channel",
            IsActive = isActive
        };
        await context.Schedules.AddAsync(schedule);
        await context.SaveChangesAsync();
        return schedule;
    }
}
