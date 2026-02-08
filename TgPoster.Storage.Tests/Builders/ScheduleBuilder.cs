using Bogus;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Tests.Builders;

internal class ScheduleBuilder(PosterContext context)
{
	private static readonly Faker faker = FakerProvider.Instance;

	private readonly Schedule schedule = new()
	{
		Id = Guid.NewGuid(),
		Name = faker.Internet.UserName(),
		UserId = new UserBuilder(context).Create().Id,
		TelegramBotId = new TelegramBotBuilder(context).Create().Id,
		ChannelId = faker.Random.Long(-199_999_999_999_999, -100_000_000_000_000),
		ChannelName = faker.Company.CompanyName(),
		IsActive = true
	};

	public Schedule Build() => schedule;

	public Schedule Create()
	{
		context.Schedules.AddRange(schedule);
		context.SaveChanges();
		return schedule;
	}

	public ScheduleBuilder WithTelegramBotId(Guid telegramBotId)
	{
		schedule.TelegramBotId = telegramBotId;
		return this;
	}

	public ScheduleBuilder WithTelegramBot(TelegramBot telegramBot)
	{
		schedule.TelegramBotId = telegramBot.Id;
		schedule.TelegramBot = telegramBot;
		return this;
	}

	public ScheduleBuilder WithUserId(Guid userId)
	{
		schedule.UserId = userId;
		return this;
	}

	public ScheduleBuilder WithUser(User user)
	{
		schedule.UserId = user.Id;
		schedule.User = user;
		return this;
	}

	public ScheduleBuilder WithYouTubeAccount(YouTubeAccount youtubeAccount)
	{
		schedule.YouTubeAccount = youtubeAccount;
		schedule.YouTubeAccountId = youtubeAccount.Id;
		return this;
	}

	public async Task<Schedule> CreateAsync(CancellationToken ct = default)
	{
		await context.Schedules.AddRangeAsync(schedule);
		await context.SaveChangesAsync(ct);
		return schedule;
	}
}