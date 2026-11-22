using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Tests.Builders;
using TgPoster.Testing;

namespace TgPoster.Storage.Tests.Tests;

public sealed class PostgreSqlTestDatabaseShould
{
	[Fact]
	public async Task CreateIsolatedDatabases_WhenInitializedConcurrently()
	{
		var databases = await Task.WhenAll(
			PostgreSqlTestDatabase.CreateAsync(GetType().Assembly),
			PostgreSqlTestDatabase.CreateAsync(GetType().Assembly));
		await using var firstDatabase = databases[0];
		await using var secondDatabase = databases[1];
		var firstAddress = new NpgsqlConnectionStringBuilder(firstDatabase.ConnectionString);
		var secondAddress = new NpgsqlConnectionStringBuilder(secondDatabase.ConnectionString);
		await using var firstContext = CreateContext(firstDatabase);
		await using var secondContext = CreateContext(secondDatabase);

		var schedule = await new ScheduleBuilder(firstContext).CreateAsync();

		firstAddress.Host.ShouldBe(secondAddress.Host);
		firstAddress.Port.ShouldBe(secondAddress.Port);
		firstAddress.Database.ShouldNotBe(secondAddress.Database);
		(await firstContext.Database.GetAppliedMigrationsAsync()).ShouldBe(firstContext.Database.GetMigrations());
		(await secondContext.Database.GetAppliedMigrationsAsync()).ShouldBe(secondContext.Database.GetMigrations());
		(await firstContext.Schedules.AnyAsync(x => x.Id == schedule.Id)).ShouldBeTrue();
		(await secondContext.Schedules.AnyAsync()).ShouldBeFalse();
	}

	[Fact]
	public async Task KeepOtherDatabasesUsable_WhenOneIsDisposed()
	{
		await using var firstDatabase = await PostgreSqlTestDatabase.CreateAsync(GetType().Assembly);
		await using var secondDatabase = await PostgreSqlTestDatabase.CreateAsync(GetType().Assembly);
		await using var secondContext = CreateContext(secondDatabase);

		await firstDatabase.DisposeAsync();
		var schedule = await new ScheduleBuilder(secondContext).CreateAsync();

		(await secondContext.Schedules.AnyAsync(x => x.Id == schedule.Id)).ShouldBeTrue();
		await using var deletedConnection = new NpgsqlConnection(firstDatabase.ConnectionString);
		var exception = await Should.ThrowAsync<PostgresException>(() => deletedConnection.OpenAsync());
		exception.SqlState.ShouldBe(PostgresErrorCodes.InvalidCatalogName);
	}

	private static PosterContext CreateContext(PostgreSqlTestDatabase database) =>
		new(new DbContextOptionsBuilder<PosterContext>().UseNpgsql(database.ConnectionString).Options,
			new TestIdentityProvider());
}
