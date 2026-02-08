using Bogus;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Tests.Builders;

internal class TelegramBotBuilder(PosterContext context)
{
	private static readonly Faker faker = FakerProvider.Instance;

	private readonly TelegramBot telegramBot = new()
	{
		Id = Guid.NewGuid(),
		ApiTelegram = faker.Company.CompanyName(),
		ChatId = faker.Random.Long(long.MinValue / 2, long.MaxValue / 2),
		OwnerId = new UserBuilder(context).Create().Id,
		Name = faker.Internet.UserName()
	};

	public TelegramBotBuilder WithOwnerId(Guid ownerId)
	{
		telegramBot.OwnerId = ownerId;
		return this;
	}

	public TelegramBot Create()
	{
		context.TelegramBots.AddRange(telegramBot);
		context.SaveChanges();
		return telegramBot;
	}

	public async Task<TelegramBot> CreateAsync(CancellationToken ct = default)
	{
		await context.TelegramBots.AddRangeAsync(telegramBot);
		await context.SaveChangesAsync(ct);
		return telegramBot;
	}
}