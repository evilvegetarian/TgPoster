using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public class ListScheduleStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly ListScheduleStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task GetListScheduleAsync_WithExistingSchedules_ShouldReturnSchedules()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var telegramBot1 = await new TelegramBotBuilder(context).WithOwnerId(user.Id).CreateAsync();
		var telegramBot2 = await new TelegramBotBuilder(context).WithOwnerId(user.Id).CreateAsync();

		var schedule1 = await new ScheduleBuilder(context).WithUserId(user.Id).WithTelegramBotId(telegramBot1.Id)
			.CreateAsync();
		schedule1.Name = "Schedule 1";
		var schedule2 = await new ScheduleBuilder(context).WithUserId(user.Id).WithTelegramBotId(telegramBot2.Id)
			.CreateAsync();
		schedule2.Name = "Schedule 2";
		await context.SaveChangesAsync();

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
		var user = await new UserBuilder(context).CreateAsync();

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
		var user1 = await new UserBuilder(context).CreateAsync();
		var user2 = await new UserBuilder(context).CreateAsync();

		var telegramBot1 = await new TelegramBotBuilder(context).WithOwnerId(user1.Id).CreateAsync();
		var telegramBot2 = await new TelegramBotBuilder(context).WithOwnerId(user2.Id).CreateAsync();

		var schedule1 = await new ScheduleBuilder(context).WithUserId(user1.Id).WithTelegramBotId(telegramBot1.Id)
			.CreateAsync();
		schedule1.Name = "User1 Schedule";
		var schedule2 = await new ScheduleBuilder(context).WithUserId(user2.Id).WithTelegramBotId(telegramBot2.Id)
			.CreateAsync();
		schedule2.Name = "User2 Schedule";
		await context.SaveChangesAsync();

		var result = await sut.GetListScheduleAsync(user1.Id, CancellationToken.None);

		result.ShouldNotBeEmpty();
		result.Count.ShouldBe(1);
		result.First().Id.ShouldBe(schedule1.Id);
		result.First().Name.ShouldBe("User1 Schedule");
	}

	[Fact]
	public async Task GetListScheduleAsync_ShouldReturnCorrectScheduleData()
	{
		var scheduleName = "Test Schedule";
		var user = await new UserBuilder(context).CreateAsync();
		var telegramBot = await new TelegramBotBuilder(context).WithOwnerId(user.Id).CreateAsync();
		var schedule = await new ScheduleBuilder(context).WithUserId(user.Id).WithTelegramBotId(telegramBot.Id)
			.CreateAsync();
		schedule.Name = scheduleName;
		schedule.IsActive = true;
		await context.SaveChangesAsync();

		var result = await sut.GetListScheduleAsync(user.Id, CancellationToken.None);
		result.ShouldNotBeEmpty();

		var returnedSchedule = result.First();
		returnedSchedule.Id.ShouldBe(schedule.Id);
		returnedSchedule.Name.ShouldBe(scheduleName);
		returnedSchedule.IsActive.ShouldBe(true);
	}
}