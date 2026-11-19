using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.Alerts;
using Shared.Telegram;
using Shouldly;
using Telegram.Bot;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Internal;
using TgPoster.Telegram.Models;

namespace Shared.Tests.Telegram;

public sealed class TelegramSessionAlertServiceShould : IDisposable
{
	private const string Token = "123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11";
	private readonly Guid id = Guid.NewGuid();
	private readonly Mock<ITelegramSessionAlertRepository> repository = new();
	private readonly TelegramBotManager manager = new();
	private readonly BotHandler handler = new();
	private readonly TelegramSessionAlertService sut;

	public TelegramSessionAlertServiceShould()
	{
		var clients = (ConcurrentDictionary<string, TelegramBotClient>)typeof(TelegramBotManager)
			.GetField("clients", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(manager)!;
		clients[Token] = new(Token, new HttpClient(handler));
		repository.Setup(x => x.GetAlertTargetAsync(id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new TelegramSessionAlertTarget(Token, 100, "Account", "+10000000000"));
		sut = new(NullLogger<TelegramSessionAlertService>.Instance, repository.Object,
			new AlertThrottle(TimeProvider.System), manager);
	}

	public void Dispose() { manager.Dispose(); handler.Dispose(); }
	public static IEnumerable<object[]> Problems() => Enum.GetValues<TelegramSessionProblem>().Select(problem => new object[] { problem });

	[Theory]
	[MemberData(nameof(Problems))]
	public async Task SendProblemDescriptionAndDetails(TelegramSessionProblem problem)
	{
		await sut.NotifyAsync(id, problem, "test details");

		handler.RequestCount.ShouldBe(1);
		handler.Text.ShouldContain("Account (+10000000000)");
		handler.Text.ShouldContain("test details");
		handler.Text.ShouldContain("Telegram");
	}

	[Fact]
	public async Task ThrottleRepeatedProblem_ButAllowDifferentProblems()
	{
		await sut.NotifyAsync(id, TelegramSessionProblem.AuthorizationRevoked);
		await sut.NotifyAsync(id, TelegramSessionProblem.AuthorizationRevoked);
		await sut.NotifyAsync(id, TelegramSessionProblem.SpamRestricted);

		handler.RequestCount.ShouldBe(2);
	}

	[Fact]
	public async Task SkipSending_WhenNotificationBotIsNotConfigured()
	{
		repository.Setup(x => x.GetAlertTargetAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((TelegramSessionAlertTarget?)null);

		await sut.NotifyAsync(id, TelegramSessionProblem.AuthorizationRevoked);

		handler.RequestCount.ShouldBe(0);
	}

	[Fact]
	public async Task UsePhoneNumber_WhenSessionHasNoName()
	{
		repository.Setup(x => x.GetAlertTargetAsync(id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new TelegramSessionAlertTarget(Token, 100, null, "+10000000000"));

		await sut.NotifyAsync(id, TelegramSessionProblem.SessionCorrupted);

		handler.Text.ShouldContain("Аккаунт: +10000000000");
		handler.Text.ShouldNotContain("Подробности:");
	}

	[Fact]
	public async Task NotInterruptCaller_WhenTelegramRejectsNotification()
	{
		handler.Reject = true;

		await Should.NotThrowAsync(() => sut.NotifyAsync(id, TelegramSessionProblem.AuthorizationRevoked));

		handler.RequestCount.ShouldBe(1);
	}

	private sealed class BotHandler : HttpMessageHandler
	{
		public int RequestCount { get; private set; }
		public string Text { get; private set; } = "";
		public bool Reject { get; set; }

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
		{
			RequestCount++;
			using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
			Text = body.RootElement.GetProperty("text").GetString()!;
			return Reject
				? new(HttpStatusCode.BadRequest) { Content = JsonContent.Create(new { ok = false, error_code = 400, description = "rejected" }) }
				: new(HttpStatusCode.OK) { Content = JsonContent.Create(new { ok = true, result = new { message_id = 1, date = 1700000000, chat = new { id = 100, type = "private", first_name = "User" } } }) };
		}
	}
}
