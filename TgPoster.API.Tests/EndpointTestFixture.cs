using System.Net.Http.Headers;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Security.Authentication;
using Security.IdentityServices;
using Shared.Telegram;
using Shared.Utilities;
using Telegram.Bot;
using Testcontainers.PostgreSql;
using TgPoster.API.Domain.Services;
using TgPoster.API.Tests.Helper;
using TgPoster.API.Tests.Seeder;
using TgPoster.Storage.Data;

namespace TgPoster.API.Tests;

public class EndpointTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
	private readonly PostgreSqlContainer dbContainer = new PostgreSqlBuilder()
		.Build();

	public string? Token;
	public HttpClient AuthClient { get; private set; } = null!;
	private readonly IIdentityProvider identityProvider = new TestIdentityProvider();

	public async Task InitializeAsync()
	{
		await dbContainer.StartAsync();
		var context = new PosterContext(new DbContextOptionsBuilder<PosterContext>()
			.UseNpgsql(dbContainer.GetConnectionString()).Options, identityProvider);
		await context.Database.MigrateAsync();
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

	internal PosterContext GetDbContext() =>
		new(new DbContextOptionsBuilder<PosterContext>()
			.UseNpgsql(dbContainer.GetConnectionString()).Options, identityProvider);

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
			ReplaceService<IBus>(services, Substitute.For<IBus>());
			ReplaceService<ITelegramAuthService>(services, Substitute.For<ITelegramAuthService>());
			ReplaceService<ITelegramChatService>(services, CreateMockChatService());
			ReplaceService<ITelegramService>(services, CreateMockTelegramService());
		});
		base.ConfigureWebHost(builder);
	}

	private static void ReplaceService<T>(IServiceCollection services, T mock) where T : class
	{
		var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(T));
		if (descriptor != null)
			services.Remove(descriptor);

		services.AddSingleton(mock);
	}

	private static ITelegramChatService CreateMockChatService()
	{
		var mock = Substitute.For<ITelegramChatService>();
		mock.GetChatInfoAsync(Arg.Any<WTelegram.Client>(), Arg.Any<string>(), Arg.Any<bool>())
			.Returns(new TelegramChatInfo
			{
				Id = 123456789L,
				AccessHash = 0L,
				Title = "Test Channel",
				Username = "testchannel",
				IsChannel = true,
				IsGroup = false,
				CanSendMessages = true,
				CanSendMedia = true,
				InputPeer = new TL.InputPeerChannel(123456789L, 0L)
			});
		mock.GetFullChannelInfoAsync(Arg.Any<WTelegram.Client>(), Arg.Any<TelegramChatInfo>())
			.Returns(new TelegramChannelInfoResult
			{
				Title = "Test Channel",
				Username = "testchannel",
				MemberCount = 100,
				IsChannel = true,
				IsGroup = false
			});
		mock.GetLinkedDiscussionGroupAsync(
				Arg.Any<WTelegram.Client>(), Arg.Any<TelegramChatInfo>(), Arg.Any<CancellationToken>())
			.Returns((123456789L, (long?)987654321L));
		return mock;
	}

	private static ITelegramService CreateMockTelegramService()
	{
		var mock = Substitute.For<ITelegramService>();
		mock.GetFileMessageInTelegramByFile(
				Arg.Any<TelegramBotClient>(),
				Arg.Any<List<IFormFile>>(),
				Arg.Any<long>(),
				Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				var files = callInfo.ArgAt<List<IFormFile>>(1);
				return files.Select(f => new MediaFileResult
				{
					MimeType = f.ContentType,
					FileId = Guid.NewGuid().ToString(),
					FileType = FileTypes.Image
				}).ToList();
			});
		mock.GetByteFileAsync(Arg.Any<TelegramBotClient>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns([0x89, 0x50, 0x4E, 0x47]);
		return mock;
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
			new TelegramSessionSeeder(context),
			new MemorySeeder(context),
			new YouTubeAccountSeeder(context)
		};

		foreach (var seeder in seeders)
		{
			await seeder.Seed();
		}

		await context.SaveChangesAsync();
	}
}

internal sealed class TestIdentityProvider : IIdentityProvider
{
	public Identity Current { get; private set; } = Identity.Anonymous;

	public void Set(Identity identity)
	{
		Current = identity;
	}
}