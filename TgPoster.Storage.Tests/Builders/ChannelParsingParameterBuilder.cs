using Bogus;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Tests.Builders;

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
			UseAiForPosts = true,
			TelegramSessionId = Guid.NewGuid()
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