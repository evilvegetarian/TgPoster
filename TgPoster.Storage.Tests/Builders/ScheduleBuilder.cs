using Bogus;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Tests.Builders;

public class ScheduleBuilder(PosterContext context)
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

	public async Task<Schedule> CreateAsync(CancellationToken ct = default)
	{
		await context.Schedules.AddRangeAsync(schedule);
		await context.SaveChangesAsync(ct);
		return schedule;
	}
}

public sealed class ChannelParsingParameterBuilder
{
	private static readonly Faker Faker = FakerProvider.Instance;

	private readonly PosterContext context;
	private readonly ChannelParsingSetting entity;

	internal ChannelParsingParameterBuilder(PosterContext context)
	{
		this.context = context;
		entity = new ChannelParsingSetting
		{
			Id = Guid.NewGuid(),
			AvoidWords = ["spam", "ban"],
			Channel = $"channel_{Faker.Random.Int(1, 10_000)}",
			DeleteMedia = true,
			DeleteText = false,
			DateFrom = DateTime.UtcNow.AddDays(-Faker.Random.Int(1, 7)),
			DateTo = DateTime.UtcNow.AddDays(Faker.Random.Int(1, 7)),
			LastParseId = Faker.Random.Int(1, 1_000_000),
			NeedVerifiedPosts = true,
			ScheduleId = new ScheduleBuilder(context).Create().Id,
			Status = Faker.Random.Enum<ParsingStatus>(),
			CheckNewPosts = Faker.Random.Bool(),
			UseAiForPosts = true
		};
	}

	internal ChannelParsingParameterBuilder WithSchedule(Schedule schedule)
	{
		entity.ScheduleId = schedule.Id;
		entity.Schedule = schedule;
		return this;
	}

	internal ChannelParsingSetting Build() => entity;

	internal async Task<ChannelParsingSetting> CreateAsync(CancellationToken ct)
	{
		await context.ChannelParsingParameters.AddAsync(entity, ct);
		await context.SaveChangesAsync(ct);
		return entity;
	}
}