using System.Net.Http.Headers;
using System.Collections.Concurrent;
using System.Reflection;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Security.Authentication;
using Security.Cryptography;
using Security.IdentityServices;
using Shared.Social.Bluesky;
using Shared.Utilities;
using Shared.Telegram;
using Telegram.Bot;
using Testcontainers.PostgreSql;
using TgPoster.API.Domain.Services;
using TgPoster.API.Tests.Helper;
using TgPoster.API.Tests.Seeder;
using TgPoster.Storage.Data;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;

namespace TgPoster.API.Tests;

public class EndpointTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
	private const string CryptoKey = "0123456789abcdef0123456789abcdef";
	private const string BotToken = "123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11";
	private readonly string contentRoot = Path.Combine(Path.GetTempPath(), "tgposter-api-tests-" + Guid.NewGuid().ToString("N"));
	private readonly PostgreSqlContainer dbContainer = new PostgreSqlBuilder()
		.Build();

	private readonly IIdentityProvider identityProvider = new TestIdentityProvider();

	public string? Token;
	public HttpClient AuthClient { get; private set; } = null!;
	public IBlueskyClient BlueskyClient { get; } = Substitute.For<IBlueskyClient>();
	public ITelegramAuthService TelegramAuth { get; } = Substitute.For<ITelegramAuthService>();

	public async Task InitializeAsync()
	{
		Directory.CreateDirectory(contentRoot);
		await File.WriteAllTextAsync(Path.Combine(contentRoot, "appsettings.json"),
			"""{"CorsPolicies":{"AllowedOrigins":["https://client.example.test"]}}""");
		await dbContainer.StartAsync();
		var context = new PosterContext(new DbContextOptionsBuilder<PosterContext>()
			.UseNpgsql(dbContainer.GetConnectionString()).Options, identityProvider);
		await context.Database.MigrateAsync();
		await InsertSeed(context);
		await CreateAuthClient();
	}

	public new async Task DisposeAsync()
	{
		await base.DisposeAsync();
		await dbContainer.DisposeAsync();
		File.Delete(Path.Combine(contentRoot, "appsettings.json"));
		Directory.Delete(contentRoot);
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
		Token = accessToken;
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
		builder.UseEnvironment("Testing");
		builder.UseContentRoot(contentRoot);
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["DataBase:ConnectionString"] = dbContainer.GetConnectionString(),
				["JwtOptions:SecretKey"] = "test-jwt-signing-key-for-isolated-http-tests-only-123456789",
				["JwtOptions:NameCookie"] = "test-access-token",
				["JwtOptions:AccessExpiresHours"] = "1",
				["JwtOptions:RefreshTokenExpiresHours"] = "1",
				["TelegramOptions:SecretKey"] = CryptoKey,
				["OpenRouterOptions:SecretKey"] = CryptoKey,
				["S3Options:AccessKey"] = "test-access",
				["S3Options:SecretKey"] = "test-secret",
				["S3Options:ServiceUrl"] = "https://files.example.test",
				["S3Options:BucketName"] = "test-bucket",
				["TracingConfiguration:Url"] = "http://127.0.0.1:4318"
			})
			.Build();
		builder.UseConfiguration(configuration);
		builder.ConfigureLogging(cfg => cfg.ClearProviders());

		builder.ConfigureServices(services =>
		{
			ReplaceService<IBus>(services, Substitute.For<IBus>());
			ReplaceService<ITelegramAuthService>(services, TelegramAuth);
			ReplaceService<ITelegramChatService>(services, CreateMockChatService());
			ReplaceService<ITelegramService>(services, CreateMockTelegramService());
			ReplaceService<IBlueskyClient>(services, BlueskyClient);
			ReplaceService(services, CreateBotManager());
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
		mock.GetChatInfoAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<bool>())
			.Returns(call => new TelegramChatInfo
			{
				Id = call.ArgAt<string>(1).StartsWith("comment_", StringComparison.Ordinal)
					? (long)(uint)call.ArgAt<string>(1).GetHashCode() + 1 : 123456789L,
				AccessHash = 0L,
				Title = "Test Channel",
				Username = "testchannel",
				IsChannel = true,
				IsGroup = false,
				CanSendMessages = true,
				CanSendMedia = true,
				Peer = TelegramPeer.Channel(123456789L, 0L)
			});
		mock.GetChatInfoAsync(Arg.Any<Guid>(), "GlobalConst.Worked.Channel", Arg.Any<bool>())
			.Returns(Task.FromException<TelegramChatInfo>(new ArgumentException("Test channel does not exist")));
		mock.GetFullChannelInfoAsync(Arg.Any<Guid>(), Arg.Any<TelegramChatInfo>())
			.Returns(new TelegramChannelInfoResult
			{
				Title = "Test Channel",
				Username = "testchannel",
				MemberCount = 100,
				IsChannel = true,
				IsGroup = false
			});
		mock.GetLinkedDiscussionGroupAsync(
				Arg.Any<Guid>(), Arg.Any<TelegramChatInfo>(), Arg.Any<CancellationToken>())
			.Returns((123456789L, (long?)987654321L));
		mock.TryGetJoinedChatsAsync(Arg.Any<Guid>())
			.Returns(TelegramOperationResult<TelegramJoinedChats>.Success(new TelegramJoinedChats
			{
				Chats = new Dictionary<long, TelegramChatInfo>
				{
					[123456789L] = new()
					{
						Id = 123456789L,
						AccessHash = 0L,
						Title = "Test Channel",
						Username = "testchannel",
						IsChannel = true,
						CanSendMessages = true,
						CanSendMedia = true,
						Peer = TelegramPeer.Channel(123456789L, 0L)
					}
				},
				ForbiddenChatIds = new HashSet<long>()
			}));
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
		var apiValue = new CryptoAES().Encrypt(CryptoKey, BotToken);
		var hash = new PasswordHasher().Generate(GlobalConst.Worked.Password);
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

	private static TelegramBotManager CreateBotManager()
	{
		var manager = new TelegramBotManager();
		var clients = (ConcurrentDictionary<string, TelegramBotClient>)typeof(TelegramBotManager)
			.GetField("clients", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager)!;
		clients[BotToken] = new TelegramBotClient(BotToken, new HttpClient(new TelegramTestHandler()));
		return manager;
	}
}

internal sealed class TelegramTestHandler : HttpMessageHandler
{
	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
	{
		var method = request.RequestUri!.Segments[^1];
		var content = request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct);
		if (content.Contains("random-channel-not-exist", StringComparison.Ordinal)
			|| content.Contains("GlobalConst.Worked.Channel", StringComparison.Ordinal))
			return new(System.Net.HttpStatusCode.BadRequest)
			{
				Content = System.Net.Http.Json.JsonContent.Create(new { ok = false, error_code = 400, description = "Bad Request: chat not found" })
			};
		object result = method.ToLowerInvariant() switch
		{
			"getchat" => new { id = GlobalConst.Worked.ChatIdTg, type = "channel", title = "Test Channel", username = "testchannel" },
			"getchatmember" => new
			{
				status = "administrator", user = new { id = 123456, is_bot = true, first_name = "Test Bot" },
				can_be_edited = true, is_anonymous = false, can_manage_chat = true, can_delete_messages = true,
				can_manage_video_chats = true, can_restrict_members = true, can_promote_members = true,
				can_change_info = true, can_invite_users = true, can_post_messages = true
			},
			_ => throw new InvalidOperationException("Unexpected Telegram request: " + method)
		};
		return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
		{
			Content = System.Net.Http.Json.JsonContent.Create(new { ok = true, result })
		};
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
