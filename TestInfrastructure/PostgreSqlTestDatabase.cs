using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Security.IdentityServices;
using Testcontainers.PostgreSql;
using TgPoster.Storage.Data;

namespace TgPoster.Testing;

public sealed class PostgreSqlTestDatabase : IAsyncDisposable
{
	private const string TemplateDatabase = "tgposter_template";
	private static readonly ConcurrentDictionary<Assembly, Lazy<Task<PostgreSqlContainer>>> containers = new();

	private PostgreSqlTestDatabase(string connectionString)
	{
		ConnectionString = connectionString;
	}

	public string ConnectionString { get; }

	public static async Task<PostgreSqlTestDatabase> CreateAsync(Assembly assembly)
	{
		var container = await containers.GetOrAdd(assembly, _ => new(CreateContainerAsync)).Value;
		var databaseName = "tests_" + Guid.NewGuid().ToString("N");
		await using var connection = CreateAdminConnection(container.GetConnectionString());
		await connection.OpenAsync();
		await using var command = new NpgsqlCommand(
			$"CREATE DATABASE \"{databaseName}\" TEMPLATE \"{TemplateDatabase}\" STRATEGY WAL_LOG", connection);
		await command.ExecuteNonQueryAsync();

		return new PostgreSqlTestDatabase(new NpgsqlConnectionStringBuilder(container.GetConnectionString())
		{
			Database = databaseName
		}.ConnectionString);
	}

	public async ValueTask DisposeAsync()
	{
		await using var databaseConnection = new NpgsqlConnection(ConnectionString);
		NpgsqlConnection.ClearPool(databaseConnection);
		await using var connection = CreateAdminConnection(ConnectionString);
		await connection.OpenAsync();
		var databaseName = new NpgsqlConnectionStringBuilder(ConnectionString).Database;
		await using var command = new NpgsqlCommand(
			$"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)", connection);
		await command.ExecuteNonQueryAsync();
	}

	public static async Task DisposeContainerAsync(Assembly assembly)
	{
		if (containers.TryRemove(assembly, out var containerTask)
			&& containerTask.IsValueCreated
			&& containerTask.Value.IsCompletedSuccessfully)
		{
			var container = await containerTask.Value;
			await container.DisposeAsync();
		}
	}

	private static async Task<PostgreSqlContainer> CreateContainerAsync()
	{
		var container = new PostgreSqlBuilder().WithDatabase(TemplateDatabase).Build();
		try
		{
			await container.StartAsync();
			var connectionString = new NpgsqlConnectionStringBuilder(container.GetConnectionString())
			{
				Pooling = false
			}.ConnectionString;
			await using var context = new PosterContext(new DbContextOptionsBuilder<PosterContext>()
				.UseNpgsql(connectionString).Options, new TemplateIdentityProvider());
			await context.Database.MigrateAsync();
			return container;
		}
		catch
		{
			await container.DisposeAsync();
			throw;
		}
	}

	private static NpgsqlConnection CreateAdminConnection(string connectionString) =>
		new(new NpgsqlConnectionStringBuilder(connectionString)
		{
			Database = "postgres",
			Pooling = false
		}.ConnectionString);

	private sealed class TemplateIdentityProvider : IIdentityProvider
	{
		public Identity Current { get; private set; } = Identity.Anonymous;

		public void Set(Identity identity)
		{
			Current = identity;
		}
	}
}
