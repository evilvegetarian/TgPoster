using System.Net.Http.Headers;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Security.Interfaces;
using Security.Models;
using Testcontainers.PostgreSql;
using TgPoster.API.Tests.Helper;
using TgPoster.API.Tests.Seeder;
using TgPoster.Storage.Data;

namespace TgPoster.API.Tests;

public class EndpointTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
	private readonly PostgreSqlContainer dbContainer = new PostgreSqlBuilder()
		.Build();

	private IMemoryCache? memoryCache;

	public string? Token;
	public HttpClient AuthClient { get; private set; } = null!;

	public async Task InitializeAsync()
	{
		await dbContainer.StartAsync();
		var context = new PosterContext(new DbContextOptionsBuilder<PosterContext>()
			.UseNpgsql(dbContainer.GetConnectionString()).Options);
		await context.Database.MigrateAsync();
		memoryCache = new MemoryCache(new MemoryCacheOptions());
		await InsertSeed(context);
		await CreateAuthClient();
	}

	public new async Task DisposeAsync()
	{
		await dbContainer.DisposeAsync();
	}

	public string GenerateTestToken(Guid userId)
	{
		using var scope = Services.CreateScope();
		var jwtProvider = scope.ServiceProvider.GetRequiredService<IJwtProvider>();
		var payload = new TokenServiceBuildTokenPayload(userId);
		return jwtProvider.GenerateToken(payload);
	}

	private Task CreateAuthClient()
	{
		AuthClient = CreateClient();
		var accessToken = GenerateTestToken(GlobalConst.Worked.UserId);
		AuthClient.DefaultRequestHeaders.Authorization =
			new AuthenticationHeaderValue("Bearer", accessToken);
		return Task.CompletedTask;
	}

	public HttpClient GetClient(string accessToken)
	{
		var client = CreateClient();
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
		return client;
	}
	public PosterContext GetDbContext() =>
		new(new DbContextOptionsBuilder<PosterContext>()
			.UseNpgsql(dbContainer.GetConnectionString()).Options);

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["DataBase:ConnectionString"] = dbContainer.GetConnectionString()
			})
			.Build();
		builder.UseConfiguration(configuration);
		builder.ConfigureLogging(cfg => cfg.ClearProviders());

		builder.ConfigureServices(services =>
		{
			services.AddSingleton(memoryCache!);

			var busDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IBus));
			if (busDescriptor != null)
			{
				services.Remove(busDescriptor);
			}

			services.AddSingleton(Substitute.For<IBus>());
		});
		base.ConfigureWebHost(builder);
	}

	private async Task InsertSeed(PosterContext context)
	{
		var configuration = new ConfigurationBuilder()
			.SetBasePath(AppContext.BaseDirectory)
			.AddJsonFile("appsettingsTest.json", false, true)
			.Build();

		var apiValue = configuration["Api"]!;

		var hash = configuration["Hash"]!;
		Token = configuration["Token"]!;
		var seeders = new BaseSeeder[]
		{
			new TelegramBotSeeder(context, apiValue),
			new MessageFileSeeder(context),
			new ScheduleSeeder(context),
			new MessageSeeder(context),
			new UserSeeder(context, hash),
			new DaySeeder(context),
			new MemorySeeder(memoryCache!)
		};

		foreach (var seeder in seeders)
		{
			await seeder.Seed();
		}

		await context.SaveChangesAsync();
	}
}