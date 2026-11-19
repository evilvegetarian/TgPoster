using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Security.Cryptography;
using Shared.Alerts;
using Shared.Telegram;
using Shouldly;
using Telegram.Bot;
using TgPoster.Worker.Domain.ConfigModels;
using TgPoster.Worker.Domain.Services.ScheduleAlerts;

namespace TgPoster.Worker.Domain.Tests.ScheduleAlerts;

public sealed class ScheduleAlertDeliveryShould : IDisposable
{
	private const string Token = "123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11";
	private readonly Guid schedule = Guid.NewGuid();
	private readonly Mock<IScheduleAlertStorage> storage = new();
	private readonly TelegramBotManager manager = new();
	private readonly BotHandler handler = new();
	private readonly ScheduleAlertService sut;

	public ScheduleAlertDeliveryShould()
	{
		var clients = (ConcurrentDictionary<string, TelegramBotClient>)typeof(TelegramBotManager)
			.GetField("clients", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(manager)!;
		clients[Token] = new(Token, new HttpClient(handler));
		storage.Setup(x => x.GetAlertTargetAsync(schedule, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new ScheduleAlertTarget("encrypted", 100, "Schedule", "channel"));
		var crypto = new Mock<ICryptoAES>();
		crypto.Setup(x => x.Decrypt("key", "encrypted")).Returns(Token);
		sut = new(NullLogger<ScheduleAlertService>.Instance, storage.Object, new AlertThrottle(TimeProvider.System),
			crypto.Object, new TelegramOptions { SecretKey = "key", TelegramSessionId = Guid.Empty }, manager);
	}

	public void Dispose() { manager.Dispose(); handler.Dispose(); }

	[Fact]
	public async Task ThrottleRepeatedProblem_AndKeepIndependentSourceKeys()
	{
		await sut.NotifyAsync(schedule, ScheduleProblem.PostFailed, "test");
		await sut.NotifyAsync(schedule, ScheduleProblem.PostFailed, "test");
		await sut.NotifyAsync(schedule, ScheduleProblem.PostFailed, "test", Guid.NewGuid());
		await sut.NotifyAsync(schedule, ScheduleProblem.PostFailed, "test", Guid.NewGuid());

		handler.RequestCount.ShouldBe(3);
	}

	[Fact]
	public async Task SkipTelegram_WhenScheduleWasDeleted()
	{
		storage.Setup(x => x.GetAlertTargetAsync(schedule, It.IsAny<CancellationToken>())).ReturnsAsync((ScheduleAlertTarget?)null);

		await sut.NotifyAsync(schedule, ScheduleProblem.QueueEmpty, "test");

		handler.RequestCount.ShouldBe(0);
	}

	[Fact]
	public async Task NotInterruptCaller_WhenNotificationFails()
	{
		handler.Reject = true;

		await Should.NotThrowAsync(() => sut.NotifyAsync(schedule, ScheduleProblem.BotLostAccess, "test"));

		handler.RequestCount.ShouldBe(1);
	}

	private sealed class BotHandler : HttpMessageHandler
	{
		public int RequestCount { get; private set; }
		public bool Reject { get; set; }
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
		{
			RequestCount++;
			return Task.FromResult(Reject
				? new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = JsonContent.Create(new { ok = false, error_code = 400, description = "rejected" }) }
				: new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { ok = true, result = new { message_id = 1, date = 1700000000, chat = new { id = 100, type = "private", first_name = "User" } } }) });
		}
	}
}
