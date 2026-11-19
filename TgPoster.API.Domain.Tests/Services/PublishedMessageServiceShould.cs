using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Telegram;
using Shouldly;
using Telegram.Bot;
using TgPoster.API.Domain.Services;

namespace TgPoster.API.Domain.Tests.Services;

public sealed class PublishedMessageServiceShould : IDisposable
{
	private const string Token = "123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11";
	private readonly TelegramBotManager manager = new();
	private readonly BotHandler handler = new();
	private readonly PublishedMessageService sut;

	public PublishedMessageServiceShould()
	{
		var clients = (ConcurrentDictionary<string, TelegramBotClient>)typeof(TelegramBotManager)
			.GetField("clients", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager)!;
		clients[Token] = new(Token, new HttpClient(handler));
		sut = new(manager, NullLogger<PublishedMessageService>.Instance);
	}

	public void Dispose() { manager.Dispose(); handler.Dispose(); }

	[Theory]
	[InlineData(false, "editmessagetext")]
	[InlineData(true, "editmessagecaption")]
	public async Task EditTextOrCaption_WithHtml(bool caption, string expectedMethod)
	{
		var result = await sut.EditTextAsync(Token, 100, 1, caption, "<b>Updated</b>", CancellationToken.None);

		result.IsSuccess.ShouldBeTrue();
		handler.LastMethod.ShouldBe(expectedMethod);
		handler.Body.ShouldContain("Updated");
		handler.Body.ShouldContain("HTML");
	}

	[Fact]
	public async Task TreatUnchangedTelegramTextAsSuccessfulEdit()
	{
		handler.Error = "Bad Request: message is not modified";

		var result = await sut.EditTextAsync(Token, 100, 1, false, "same", CancellationToken.None);

		result.IsSuccess.ShouldBeTrue();
	}

	[Fact]
	public async Task ReturnFailure_WhenTelegramRejectsEdit()
	{
		handler.Error = "Bad Request: message can't be edited";

		var result = await sut.EditTextAsync(Token, 100, 1, false, "changed", CancellationToken.None);

		result.IsSuccess.ShouldBeFalse();
		result.Error.ShouldNotBeNull();
		result.Error.ShouldContain("can't be edited");
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ReturnDeletionOutcome(bool rejected)
	{
		if (rejected) handler.Error = "Bad Request: message can't be deleted";

		var result = await sut.DeleteAsync(Token, 100, [1, 2], CancellationToken.None);

		result.IsSuccess.ShouldBe(!rejected);
		handler.LastMethod.ShouldBe("deletemessages");
		handler.Body.ShouldContain("1");
		handler.Body.ShouldContain("2");
	}

	private sealed class BotHandler : HttpMessageHandler
	{
		public string? Error { get; set; }
		public string? LastMethod { get; private set; }
		public string Body { get; private set; } = "";

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
		{
			LastMethod = request.RequestUri!.Segments[^1].ToLowerInvariant();
			Body = await request.Content!.ReadAsStringAsync(ct);
			if (Error is not null) return new(HttpStatusCode.BadRequest)
			{
				Content = JsonContent.Create(new { ok = false, error_code = 400, description = Error })
			};
			object result = LastMethod == "deletemessages" ? true
				: new { message_id = 1, date = 1700000000, chat = new { id = 100, type = "channel", title = "Test" } };
			return new(HttpStatusCode.OK) { Content = JsonContent.Create(new { ok = true, result }) };
		}
	}
}
