using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using TgPoster.API;
using TgPoster.Storage.Data;

namespace TgPoster.E2E;

public class EndpointTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
	private readonly PostgreSqlContainer dbContainer = new PostgreSqlBuilder().Build();

	public async Task InitializeAsync()
	{
		await dbContainer.StartAsync();
		var forumDbContext = new PosterContext(new DbContextOptionsBuilder<PosterContext>()
			.UseNpgsql(dbContainer.GetConnectionString()).Options);
		await forumDbContext.Database.MigrateAsync();
	}

	public new async Task DisposeAsync()
	{
		await dbContainer.DisposeAsync();
	}

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new[]
			{
				new KeyValuePair<string, string>("DataBase:ConnectionString",
					dbContainer.GetConnectionString())
			}!)
			.Build();
		builder.UseConfiguration(configuration);
		builder.ConfigureLogging(cfg => cfg.ClearProviders());
		base.ConfigureWebHost(builder);
	}
}