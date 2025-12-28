using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public class ListPromptSettingStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly CancellationToken ct = CancellationToken.None;
	private readonly ListPromptSettingStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task GetAsync_WithExist_ShouldValidReturn()
	{
		var user = new UserBuilder(context).Create();
		var schedule1 = new ScheduleBuilder(context).WithUser(user).Create();
		var setting1 = new PromptSettingBuilder(context).WithSchedule(schedule1).Create();
		var schedule2 = new ScheduleBuilder(context).WithUser(user).Create();
		var setting2 = new PromptSettingBuilder(context).WithSchedule(schedule2).Create();

		var response = await sut.GetAsync(user.Id, ct);
		response.Count.ShouldBe(2);
	}


	[Fact]
	public async Task GetAsync_WithNotExistUserId_ShouldValidReturnEmpty()
	{
		var response = await sut.GetAsync(Guid.NewGuid(), ct);
		response.Count.ShouldBe(0);
	}
}