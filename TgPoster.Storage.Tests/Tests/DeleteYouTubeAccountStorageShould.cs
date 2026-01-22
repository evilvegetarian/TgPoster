using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public class DeleteYouTubeAccountStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly DeleteYouTubeAccountStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task ExistsAsync_WithExistingYouTubeAccount_ShouldReturnTrue()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var youtubeAccount = new YouTubeAccountBuilder(context).WithUser(user).Create();

		var result = await sut.ExistsAsync(youtubeAccount.Id, user.Id, CancellationToken.None);

		result.ShouldBeTrue();
	}

	[Fact]
	public async Task ExistsAsync_WithNonExistingYouTubeAccount_ShouldReturnFalse()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var nonExistingAccountId = Guid.NewGuid();

		var result = await sut.ExistsAsync(nonExistingAccountId, user.Id, CancellationToken.None);

		result.ShouldBeFalse();
	}

	[Fact]
	public async Task ExistsAsync_WithWrongUserId_ShouldReturnFalse()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var youtubeAccount = new YouTubeAccountBuilder(context).WithUser(user).Create();
		var wrongUserId = Guid.NewGuid();

		var result = await sut.ExistsAsync(youtubeAccount.Id, wrongUserId, CancellationToken.None);

		result.ShouldBeFalse();
	}

	[Fact]
	public async Task DeleteYouTubeAccountAsync_WithExistingAccount_ShouldDeleteAccount()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var youtubeAccount = new YouTubeAccountBuilder(context).WithUser(user).Create();

		await sut.DeleteYouTubeAccountAsync(youtubeAccount.Id, user.Id, CancellationToken.None);

		var deletedAccount = await context.YouTubeAccounts
			.FirstOrDefaultAsync(x => x.Id == youtubeAccount.Id);

		deletedAccount.ShouldBeNull();
	}

	[Fact]
	public async Task DeleteYouTubeAccountAsync_WithWrongUserId_ShouldNotDeleteAccount()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var youtubeAccount = new YouTubeAccountBuilder(context).WithUser(user).Create();
		var wrongUserId = Guid.NewGuid();

		await sut.DeleteYouTubeAccountAsync(youtubeAccount.Id, wrongUserId, CancellationToken.None);

		var account = await context.YouTubeAccounts
			.FirstOrDefaultAsync(x => x.Id == youtubeAccount.Id);

		account.ShouldNotBeNull();
	}

	[Fact]
	public async Task DeleteYouTubeAccountAsync_WithLinkedSchedule_ShouldSetScheduleYouTubeAccountIdToNull()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var youtubeAccount = new YouTubeAccountBuilder(context).WithUser(user).Create();
		var telegramBot = new TelegramBotBuilder(context).Create();
		var schedule = new ScheduleBuilder(context).WithUser(user).WithTelegramBot(telegramBot)
			.WithYouTubeAccount(youtubeAccount).Create();

		await sut.DeleteYouTubeAccountAsync(youtubeAccount.Id, user.Id, CancellationToken.None);

		var updatedSchedule = await context.Schedules
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == schedule.Id);

		updatedSchedule.ShouldNotBeNull();
		updatedSchedule.YouTubeAccountId.ShouldBeNull();
	}

	[Fact]
	public async Task
		DeleteYouTubeAccountAsync_WithMultipleLinkedSchedules_ShouldSetAllSchedulesYouTubeAccountIdToNull()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var youtubeAccount = new YouTubeAccountBuilder(context).WithUser(user).Create();
		var telegramBot = new TelegramBotBuilder(context).Create();

		var schedule1 = new ScheduleBuilder(context).WithUser(user).WithTelegramBot(telegramBot)
			.WithYouTubeAccount(youtubeAccount).Create();
		var schedule2 = new ScheduleBuilder(context).WithUser(user).WithTelegramBot(telegramBot)
			.WithYouTubeAccount(youtubeAccount).Create();

		await sut.DeleteYouTubeAccountAsync(youtubeAccount.Id, user.Id, CancellationToken.None);

		var updatedSchedule1 = await context.Schedules
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == schedule1.Id);

		var updatedSchedule2 = await context.Schedules
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == schedule2.Id);

		updatedSchedule1.ShouldNotBeNull();
		updatedSchedule1.YouTubeAccountId.ShouldBeNull();

		updatedSchedule2.ShouldNotBeNull();
		updatedSchedule2.YouTubeAccountId.ShouldBeNull();
	}

	[Fact]
	public async Task DeleteYouTubeAccountAsync_ShouldNotAffectSchedulesWithDifferentYouTubeAccount()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var youtubeAccount1 = new YouTubeAccountBuilder(context).WithUser(user).Create();
		var youtubeAccount2 = new YouTubeAccountBuilder(context).WithUser(user).Create();
		var telegramBot = new TelegramBotBuilder(context).Create();

		var scheduleWithAccount1 = new ScheduleBuilder(context).WithUser(user).WithTelegramBot(telegramBot)
			.WithYouTubeAccount(youtubeAccount1).Create();
		var scheduleWithAccount2 = new ScheduleBuilder(context).WithUser(user).WithTelegramBot(telegramBot)
			.WithYouTubeAccount(youtubeAccount2).Create();

		await sut.DeleteYouTubeAccountAsync(youtubeAccount1.Id, user.Id, CancellationToken.None);

		var updatedScheduleWithAccount1 = await context.Schedules
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == scheduleWithAccount1.Id);

		var updatedScheduleWithAccount2 = await context.Schedules
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == scheduleWithAccount2.Id);

		updatedScheduleWithAccount1.ShouldNotBeNull();
		updatedScheduleWithAccount1.YouTubeAccountId.ShouldBeNull();

		updatedScheduleWithAccount2.ShouldNotBeNull();
		updatedScheduleWithAccount2.YouTubeAccountId.ShouldBe(youtubeAccount2.Id);
	}
}