using Bogus;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Tests.Builders;

public sealed class RefreshSessionBuilder(PosterContext context)
{
	private static readonly Faker faker = FakerProvider.Instance;

	private readonly RefreshSession refreshSession = new()
	{
		Id = Guid.NewGuid(),
		UserId = new UserBuilder(context).Create().Id,
		RefreshToken = Guid.NewGuid(),
		ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
	};

	public RefreshSessionBuilder WithUserId(Guid userId)
	{
		refreshSession.UserId = userId;
		return this;
	}

	public RefreshSessionBuilder WithRefreshToken(Guid refreshToken)
	{
		refreshSession.RefreshToken = refreshToken;
		return this;
	}

	public RefreshSessionBuilder WithExpiresAt(DateTimeOffset expiresAt)
	{
		refreshSession.ExpiresAt = expiresAt;
		return this;
	}

	public RefreshSession Build() => refreshSession;

	public RefreshSession Create()
	{
		context.RefreshSessions.Add(refreshSession);
		context.SaveChanges();
		return refreshSession;
	}

	public async Task<RefreshSession> CreateAsync(CancellationToken ct = default)
	{
		await context.RefreshSessions.AddAsync(refreshSession, ct);
		await context.SaveChangesAsync(ct);
		return refreshSession;
	}
}
