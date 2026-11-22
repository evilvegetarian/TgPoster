using Microsoft.EntityFrameworkCore;
using Security.IdentityServices;
using TgPoster.Storage.Data;
using TgPoster.Testing;

namespace TgPoster.Storage.Tests;

public class StorageTestFixture : IAsyncLifetime
{
	private readonly IIdentityProvider identityProvider = new TestIdentityProvider();
	private PostgreSqlTestDatabase database = null!;

	public virtual async Task InitializeAsync()
	{
		database = await PostgreSqlTestDatabase.CreateAsync(GetType().Assembly);
	}

	public async Task DisposeAsync()
	{
		if (database is not null)
			await database.DisposeAsync();
	}

	internal PosterContext GetDbContext() =>
		new(new DbContextOptionsBuilder<PosterContext>()
			.UseNpgsql(database.ConnectionString).Options, identityProvider);
}

internal sealed class TestIdentityProvider : IIdentityProvider
{
	public Identity Current { get; private set; } = Identity.Anonymous;

	public void Set(Identity identity)
	{
		Current = identity;
	}
}
