using Bogus;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Tests.Builders;

internal sealed class TelegramSessionChatBanBuilder(PosterContext context)
{
	private static readonly Faker faker = FakerProvider.Instance;

	private readonly TelegramSessionChatBan ban = new()
	{
		Id = Guid.NewGuid(),
		TelegramSessionId = new TelegramSessionBuilder(context).Create().Id,
		ChatId = faker.Random.Long(1_000_000, long.MaxValue),
		Error = "CHANNEL_PRIVATE",
		DetectedAt = DateTimeOffset.UtcNow
	};

	public TelegramSessionChatBan Build() => ban;

	public async Task<TelegramSessionChatBan> CreateAsync(CancellationToken ct = default)
	{
		await context.AddAsync(ban, ct);
		await context.SaveChangesAsync(ct);
		return ban;
	}

	public TelegramSessionChatBanBuilder WithTelegramSessionId(Guid telegramSessionId)
	{
		ban.TelegramSessionId = telegramSessionId;
		return this;
	}

	public TelegramSessionChatBanBuilder WithChatId(long chatId)
	{
		ban.ChatId = chatId;
		return this;
	}

	public TelegramSessionChatBanBuilder WithDeleted(DateTimeOffset? deleted)
	{
		ban.Deleted = deleted;
		return this;
	}
}
