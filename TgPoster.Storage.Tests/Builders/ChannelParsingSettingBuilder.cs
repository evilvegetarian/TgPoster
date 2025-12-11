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

	public ChannelParsingSetting Build() => setting;

	public ChannelParsingSetting Create()
	{
		context.ChannelParsingParameters.AddRange(setting);
		context.SaveChanges();
		return setting;
	}
}