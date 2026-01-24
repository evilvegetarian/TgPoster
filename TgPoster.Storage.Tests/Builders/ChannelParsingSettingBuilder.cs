using Bogus;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Tests.Builders;

public class ChannelParsingSettingBuilder(PosterContext context)
{
	private static readonly Faker faker = FakerProvider.Instance;

	private readonly ChannelParsingSetting setting = new()
	{
		Id = Guid.NewGuid(),
		UseAiForPosts = true,
		AvoidWords = ["spam", "ban"],
		Channel = $"channel_{faker.Random.Int(1, 10_000)}",
		DeleteMedia = true,
		DeleteText = false,
		DateFrom = DateTime.UtcNow.AddDays(-faker.Random.Int(1, 7)),
		DateTo = DateTime.UtcNow.AddDays(faker.Random.Int(1, 7)),
		LastParseId = faker.Random.Int(1, 1_000_000),
		NeedVerifiedPosts = true,
		ScheduleId = new ScheduleBuilder(context).Create().Id,
		Status = faker.Random.Enum<ParsingStatus>(),
		CheckNewPosts = faker.Random.Bool()
	};

	public ChannelParsingSettingBuilder WithScheduleId(Guid scheduleId)
	{
		setting.ScheduleId = scheduleId;
		return this;
	}

	public ChannelParsingSettingBuilder WithStatus(ParsingStatus status)
	{
		setting.Status = status;
		return this;
	}

	public ChannelParsingSettingBuilder WithCheckNewPosts(bool checkNewPosts)
	{
		setting.CheckNewPosts = checkNewPosts;
		return this;
	}

	public ChannelParsingSetting Build() => setting;

	public ChannelParsingSetting Create()
	{
		context.ChannelParsingParameters.AddRange(setting);
		context.SaveChanges();
		return setting;
	}

	public async Task<ChannelParsingSetting> CreateAsync(CancellationToken ct = default)
	{
		await context.ChannelParsingParameters.AddRangeAsync(setting);
		await context.SaveChangesAsync(ct);
		return setting;
	}
}