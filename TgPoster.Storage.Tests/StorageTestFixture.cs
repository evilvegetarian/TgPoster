using Microsoft.EntityFrameworkCore;
using Security.IdentityServices;
using Testcontainers.PostgreSql;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Tests;

public class StorageTestFixture : IAsyncLifetime
{
	private readonly PostgreSqlContainer dbContainer = new PostgreSqlBuilder().Build();
	private PosterContext PosterContext { get; set; } = null!;
	private readonly IIdentityProvider identityProvider = new TestIdentityProvider();

	public virtual async Task InitializeAsync()
	{
		await dbContainer.StartAsync();
		var forumDbContext = new PosterContext(new DbContextOptionsBuilder<PosterContext>()
			.UseNpgsql(dbContainer.GetConnectionString()).Options, identityProvider);
		PosterContext = forumDbContext;
		await forumDbContext.Database.MigrateAsync();
	}

	public async Task DisposeAsync()
	{
		await dbContainer.StopAsync();
	}

	internal PosterContext GetDbContext() =>
		new(new DbContextOptionsBuilder<PosterContext>()
			.UseNpgsql(dbContainer.GetConnectionString()).Options, identityProvider);
}

internal sealed class TestIdentityProvider : IIdentityProvider
{
	public Identity Current { get; private set; } = Identity.Anonymous;

	public void Set(Identity identity)
	{
		Current = identity;
	}
}