using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public class GetPromptSettingStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly CancellationToken ct = CancellationToken.None;
	private readonly GetPromptSettingStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task GetAsync_WithExist_ShouldValidReturn()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync(ct);
		var promptSetting = new PromptSettingBuilder(context).WithSchedule(schedule).Create();
		var response = await sut.GetAsync(promptSetting.Id, schedule.UserId, ct);
		response.Id.ShouldBe(promptSetting.Id);
		response.PicturePrompt.ShouldBe(promptSetting.PicturePrompt);
	}

	[Fact]
	public async Task GetAsync_WithExistId_ShouldReturnNull()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync(ct);
		new PromptSettingBuilder(context).WithScheduleId(schedule.Id).Create();
		var response = await sut.GetAsync(Guid.NewGuid(), schedule.UserId, ct);
		response.ShouldBeNull();
	}


	[Fact]
	public async Task GetAsync_WithNotExistUserId_ShouldValidReturnNull()
	{
		var response = await sut.GetAsync(Guid.NewGuid(), Guid.NewGuid(), ct);
		response.ShouldBeNull();
	}
}