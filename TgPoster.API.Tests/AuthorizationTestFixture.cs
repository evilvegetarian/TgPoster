using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Security;
using Security.Authentication;
using Security.Cryptography;
using Security.Middleware;
using Telegram.Bot;
using TgPoster.API.Controllers;
using TgPoster.API.Domain;
using TgPoster.API.Domain.Services;
using TgPoster.API.Domain.UseCases.Files.UploadFileToS3;
using TgPoster.API.Domain.UseCases.Messages.CreateMessagesFromFiles;
using TgPoster.API.Domain.UseCases.Messages.GetMessageById;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;
using TgPoster.API.Domain.UseCases.YouTubeAccount.CallBackYouTube;
using TgPoster.API.Domain.UseCases.YouTubeAccount.YouTubeAccountLogin;
using TgPoster.Exceptions;

namespace TgPoster.API.Tests;

public sealed class AuthorizationTestFixture : IAsyncLifetime
{
	public static readonly Guid OwnerId = Guid.NewGuid();
	public static readonly Guid FileId = Guid.NewGuid();
	public static readonly Guid MessageId = Guid.NewGuid();
	public static readonly Guid YouTubeAccountId = Guid.NewGuid();
	public IUploadFileToS3Storage Files { get; } = Substitute.For<IUploadFileToS3Storage>();
	public ICallBackYouTubeStorage YouTubeCallbacks { get; } = Substitute.For<ICallBackYouTubeStorage>();
	public TestClock Clock { get; } = new();
	public IServiceProvider Services => host.Services;
	private IHost host = null!;

	public async Task InitializeAsync()
	{
		var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
		{
			["JwtOptions:SecretKey"] = "test-jwt-signing-key-for-isolated-http-tests-only-123456789",
			["JwtOptions:NameCookie"] = "test-access-token",
			["JwtOptions:AccessExpiresHours"] = "1",
			["JwtOptions:RefreshTokenExpiresHours"] = "1",
			["TelegramOptions:SecretKey"] = "test-file-signing-key",
			["OpenRouterOptions:SecretKey"] = "test-router-key",
			["S3Options:AccessKey"] = "test-access",
			["S3Options:SecretKey"] = "test-secret",
			["S3Options:ServiceUrl"] = "https://files.example.test",
			["S3Options:BucketName"] = "bucket"
		}).Build();
		Files.GetFileInfoAsync(FileId, Arg.Any<CancellationToken>()).Returns(new FileInfoDto
		{
			UserId = OwnerId, TgFileId = "test-file", ContentType = "image/jpeg", IsInS3 = true
		});
		var fileStorage = Substitute.For<IFileStorage>();
		var messageStorage = Substitute.For<IGetMessageStorage>();
		messageStorage.GetMessagesAsync(MessageId, OwnerId, Arg.Any<CancellationToken>()).Returns(new MessageDto
		{
			Id = MessageId, ScheduleId = Guid.NewGuid(), TimePosting = Clock.Now,
			DeliveryStatus = MessageDeliveryStatus.Planned,
			Files = [new FileDto { Id = FileId, ContentType = "image/jpeg", TgFileId = "test-file" }]
		});
		fileStorage.GetFileInfoAsync(FileId, Arg.Any<CancellationToken>()).Returns(new MessageFileInfo
		{
			MessageId = MessageId, ContentType = "image/jpeg", TgFileId = "test-file"
		});
		var botStorage = Substitute.For<IGetTelegramBotStorage>();
		botStorage.GetTelegramBotByMessageIdAsync(MessageId, OwnerId, Arg.Any<CancellationToken>())
			.Returns(new TelegramBotDto { ApiTelegram = "encrypted", ChatId = 100 });
		var telegram = Substitute.For<ITelegramService>();
		telegram.GetByteFileAsync(Arg.Any<TelegramBotClient>(), "test-file", Arg.Any<CancellationToken>())
			.Returns([0x89, 0x50, 0x4e, 0x47]);
		var crypto = Substitute.For<ICryptoAES>();
		crypto.Decrypt(Arg.Any<string>(), "encrypted").Returns("123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11");
		var loginStorage = Substitute.For<ILoginYouTubeStorage>();
		loginStorage.CreateYouTubeAccountAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
			OwnerId, Arg.Any<CancellationToken>()).Returns(YouTubeAccountId);

		host = await new HostBuilder().ConfigureWebHost(web => web.UseTestServer()
			.ConfigureServices(services =>
			{
				services.AddLogging();
				services.AddControllers().AddApplicationPart(typeof(FileController).Assembly);
				services.AddHttpContextAccessor();
				services.AddMemoryCache();
				services.AddSingleton<FileExtensionContentTypeProvider>();
				services.AddSingleton<TimeProvider>(Clock);
				services.AddDomain(configuration).AddSecurity(configuration);
				services.AddSingleton(Files);
				services.AddSingleton(fileStorage);
				services.AddSingleton(messageStorage);
				services.AddSingleton(botStorage);
				services.AddSingleton(YouTubeCallbacks);
				services.AddSingleton(loginStorage);
				services.RemoveAll<ITelegramService>();
				services.AddSingleton(telegram);
				services.RemoveAll<ICryptoAES>();
				services.AddSingleton(crypto);
			})
			.Configure(app =>
			{
				app.UseTgPosterExceptionHandling();
				app.UseRouting();
				app.UseAuthentication();
				app.UseMiddleware<AuthenticationMiddleware>();
				app.UseAuthorization();
				app.UseEndpoints(endpoints => endpoints.MapControllers());
			})).StartAsync();
	}

	public Task DisposeAsync()
	{
		host.Dispose();
		return Task.CompletedTask;
	}

	public HttpClient GetClient(Guid? userId = null)
	{
		var client = host.GetTestClient();
		if (userId is { } id)
		{
			using var scope = host.Services.CreateScope();
			var token = scope.ServiceProvider.GetRequiredService<IJwtProvider>()
				.GenerateToken(new TokenServiceBuildTokenPayload(id));
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
		}
		return client;
	}

	public sealed class TestClock : TimeProvider
	{
		public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
		public override DateTimeOffset GetUtcNow() => Now;
	}
}
