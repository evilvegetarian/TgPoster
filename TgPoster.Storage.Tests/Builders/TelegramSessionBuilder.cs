using Bogus;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Tests.Builders;

public sealed class TelegramSessionBuilder(PosterContext context)
{
	private static readonly Faker faker = FakerProvider.Instance;

	private readonly TelegramSession telegramSession = new()
	{
		Id = Guid.NewGuid(),
		ApiId = faker.Random.Int(100000, 999999).ToString(),
		ApiHash = faker.Random.AlphaNumeric(32),
		PhoneNumber = $"+{faker.Random.Long(10000000000, 99999999999)}",
		Name = faker.Internet.UserName(),
		IsActive = true,
		Status = TelegramSessionStatus.AwaitingCode,
		UserId = new UserBuilder(context).Create().Id
	};

	public TelegramSessionBuilder WithUserId(Guid userId)
	{
		telegramSession.UserId = userId;
		return this;
	}

	public TelegramSessionBuilder WithApiId(string apiId)
	{
		telegramSession.ApiId = apiId;
		return this;
	}

	public TelegramSessionBuilder WithApiHash(string apiHash)
	{
		telegramSession.ApiHash = apiHash;
		return this;
	}

	public TelegramSessionBuilder WithPhoneNumber(string phoneNumber)
	{
		telegramSession.PhoneNumber = phoneNumber;
		return this;
	}

	public TelegramSessionBuilder WithName(string? name)
	{
		telegramSession.Name = name;
		return this;
	}

	public TelegramSessionBuilder WithIsActive(bool isActive)
	{
		telegramSession.IsActive = isActive;
		return this;
	}

	public TelegramSessionBuilder WithStatus(TelegramSessionStatus status)
	{
		telegramSession.Status = status;
		return this;
	}

	public TelegramSessionBuilder WithSessionData(string? sessionData)
	{
		telegramSession.SessionData = sessionData;
		return this;
	}

	public TelegramSessionBuilder WithCreated(DateTime created)
	{
		telegramSession.Created = created;
		return this;
	}

	public TelegramSession Build() => telegramSession;

	public TelegramSession Create()
	{
		context.TelegramSessions.Add(telegramSession);
		context.SaveChanges();
		return telegramSession;
	}

	public async Task<TelegramSession> CreateAsync(CancellationToken ct = default)
	{
		await context.TelegramSessions.AddAsync(telegramSession, ct);
		await context.SaveChangesAsync(ct);
		return telegramSession;
	}
}
