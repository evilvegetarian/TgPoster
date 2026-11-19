using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.TelegramSessions.CreateTelegramSession;
using TgPoster.API.Domain.UseCases.TelegramSessions.ListTelegramSessions;
using TgPoster.API.Domain.UseCases.TelegramSessions.VerifyCode;
using TgPoster.API.Models;
using TgPoster.Telegram.Models;

namespace TgPoster.API.Tests.Endpoint;

public sealed class TelegramSessionEndpointShould : IClassFixture<EndpointTestFixture>
{
	private const string Url = Routes.TelegramSession.Root;
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
	{
		Converters = { new JsonStringEnumConverter() }
	};
	private readonly EndpointTestFixture fixture;
	private readonly HttpClient client;

	public TelegramSessionEndpointShould(EndpointTestFixture fixture)
	{
		this.fixture = fixture;
		client = fixture.AuthClient;
		fixture.TelegramAuth.StartAuthAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns("code_sent");
		fixture.TelegramAuth.SendPasswordAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
	}

	[Fact]
	public async Task CreateSessionWithOwnedProxy_AndReconnectItWhenProxyChangesOrIsDeleted()
	{
		var proxyResponse = await client.PostAsJsonAsync(Routes.Proxy.Root,
			new { name = "test-proxy", type = "Http", host = "proxy.example.test", port = 8080 });
		proxyResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
		var proxy = (await proxyResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("id").GetGuid();
		var createdResponse = await client.PostAsJsonAsync(Url,
			new CreateTelegramSessionRequest("123", "private-api-hash", "+10000000000", "proxied", proxy, null));
		createdResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
		var session = (await createdResponse.Content.ReadFromJsonAsync<CreateTelegramSessionResponse>())!;

		var changed = await client.PutAsJsonAsync($"{Routes.Proxy.Root}/{proxy}",
			new { name = "updated-proxy", type = "Http", host = "other.example.test", port = 8081 });
		var removed = await client.DeleteAsync($"{Routes.Proxy.Root}/{proxy}");

		changed.StatusCode.ShouldBe(HttpStatusCode.NoContent);
		removed.StatusCode.ShouldBe(HttpStatusCode.NoContent);
		await fixture.TelegramAuth.Received(2).RemoveClientAsync(session.Id);
		await using var context = fixture.GetDbContext();
		(await context.TelegramSessions.FirstAsync(x => x.Id == session.Id)).ProxyId.ShouldBeNull();
	}

	[Fact]
	public async Task CreateListAndDeleteSession_WithoutExposingCredentials()
	{
		var created = await Create();

		var list = await client.GetFromJsonAsync<TelegramSessionListResponse>(Url, JsonOptions);
		var response = await client.GetAsync(Url);
		var body = await response.Content.ReadAsStringAsync();

		created.AuthStatus.ShouldBe("code_sent");
		list.ShouldNotBeNull();
		list.Items.ShouldContain(x => x.Id == created.Id && x.Name == "test-session");
		body.ShouldNotContain("private-api-hash");
		body.ShouldNotContain("sessionData");
		(await client.DeleteAsync($"{Url}/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
		(await client.GetFromJsonAsync<TelegramSessionListResponse>(Url, JsonOptions))!.Items.ShouldNotContain(x => x.Id == created.Id);
	}

	[Theory]
	[InlineData(true, false)]
	[InlineData(false, true)]
	[InlineData(false, false)]
	public async Task ReturnCodeVerificationOutcome(bool success, bool passwordRequired)
	{
		var created = await Create();
		fixture.TelegramAuth.VerifyCodeAsync(created.Id, "12345", Arg.Any<CancellationToken>())
			.Returns(new VerifyCodeResult { Success = success, RequiresPassword = passwordRequired, Message = "test-result" });

		var response = await client.PostAsJsonAsync($"{Url}/{created.Id}/verify-code", new VerifyCodeRequest("12345"));

		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		var result = await response.Content.ReadFromJsonAsync<VerifyCodeResponse>();
		result.ShouldNotBeNull();
		result.Success.ShouldBe(success);
		result.RequiresPassword.ShouldBe(passwordRequired);
		result.Message.ShouldBe("test-result");
	}

	[Theory]
	[InlineData("start-auth")]
	[InlineData("verify-code")]
	[InlineData("send-password")]
	public async Task RejectForeignSession_BeforeCallingTelegram(string operation)
	{
		var created = await Create();
		using var foreign = fixture.GetClient(fixture.GenerateTestToken(Guid.NewGuid()));
		fixture.TelegramAuth.ClearReceivedCalls();

		var response = await foreign.PostAsJsonAsync($"{Url}/{created.Id}/{operation}", new { code = "12345", password = "password" });

		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
		fixture.TelegramAuth.ReceivedCalls().ShouldBeEmpty();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task ReturnPasswordVerificationOutcome(bool success)
	{
		var created = await Create();
		fixture.TelegramAuth.SendPasswordAsync(created.Id, "password", Arg.Any<CancellationToken>()).Returns(success);

		var response = await client.PostAsJsonAsync($"{Url}/{created.Id}/send-password", new { password = "password" });

		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
		result.GetProperty("success").GetBoolean().ShouldBe(success);
	}

	[Fact]
	public async Task DeactivateSession_AndRemoveItsCachedClient()
	{
		var created = await Create();

		var response = await client.PutAsJsonAsync($"{Url}/{created.Id}", new UpdateTelegramSessionRequest("disabled", false, null, null));

		response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
		await fixture.TelegramAuth.Received().RemoveClientAsync(created.Id);
		await using var context = fixture.GetDbContext();
		var stored = await context.TelegramSessions.FirstAsync(x => x.Id == created.Id);
		stored.IsActive.ShouldBeFalse();
		stored.Name.ShouldBe("disabled");
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task ImportSession_OnlyWhenTelegramAcceptsIt(bool accepted)
	{
		fixture.TelegramAuth.ImportSessionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<MemoryStream>(), Arg.Any<CancellationToken>())
			.Returns(new ImportSessionResult { Success = accepted, PhoneNumber = "+10000000000", ErrorMessage = accepted ? null : "invalid session" });
		using var form = new MultipartFormDataContent
		{
			{ new StringContent("123"), "ApiId" }, { new StringContent("private-api-hash"), "ApiHash" },
			{ new StringContent("imported-session"), "Name" }, { new ByteArrayContent([1, 2, 3]), "SessionFile", "test.session" }
		};

		var response = await client.PostAsync(Url + "/import", form);

		response.StatusCode.ShouldBe(accepted ? HttpStatusCode.Created : HttpStatusCode.BadRequest);
		if (!accepted) return;
		var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
		var id = result.GetProperty("id").GetGuid();
		await using var context = fixture.GetDbContext();
		var session = await context.TelegramSessions.FirstAsync(x => x.Id == id);
		session.SessionData.ShouldBe("AQID");
		session.Status.ToString().ShouldBe("Authorized");
	}

	private async Task<CreateTelegramSessionResponse> Create()
	{
		var response = await client.PostAsJsonAsync(Url,
			new CreateTelegramSessionRequest("123", "private-api-hash", "+10000000000", "test-session", null, null));
		response.StatusCode.ShouldBe(HttpStatusCode.Created);
		return (await response.Content.ReadFromJsonAsync<CreateTelegramSessionResponse>())!;
	}
}
