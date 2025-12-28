using Bogus;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Tests.Builders;

public class TelegramBotBuilder(PosterContext context)
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

	public TelegramBot Build() => telegramBot;

	public TelegramBot Create()
	{
		context.TelegramBots.AddRange(telegramBot);
		context.SaveChanges();
		return telegramBot;
	}
}