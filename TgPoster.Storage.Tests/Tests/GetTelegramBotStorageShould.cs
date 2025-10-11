using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;

namespace TgPoster.Storage.Tests.Tests;

public class GetTelegramBotStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
    private readonly PosterContext context = fixture.GetDbContext();
    private readonly Helper helper = new(fixture.GetDbContext());
    private readonly GetTelegramBotStorage sut = new(fixture.GetDbContext());

    [Fact]
    public async Task GetApiTokenAsync_WithExistingSchedule_ShouldReturnApiToken()
    {
        var schedule = await helper.CreateScheduleAsync();

        var result = await sut.GetApiTokenAsync(schedule.Id, schedule.UserId, CancellationToken.None);

        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetApiTokenAsync_WithNonExistingSchedule_ShouldReturnNull()
    {
        var user = await helper.CreateUserAsync();
        var nonExistingScheduleId = Guid.NewGuid();

        var result = await sut.GetApiTokenAsync(nonExistingScheduleId, user.Id, CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetApiTokenAsync_WithWrongUserId_ShouldReturnNull()
    {
        var schedule = await helper.CreateScheduleAsync();
        var wrongUserId = Guid.NewGuid();

        var result = await sut.GetApiTokenAsync(schedule.Id, wrongUserId, CancellationToken.None);

        result.ShouldBeNull();
    }
}