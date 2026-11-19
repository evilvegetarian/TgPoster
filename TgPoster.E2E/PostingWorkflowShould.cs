using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TgPoster.API.Tests.Helper;

namespace TgPoster.E2E;

public sealed class PostingWorkflowShould(EndpointTestFixture fixture) : IClassFixture<EndpointTestFixture>
{
	[Fact]
	public async Task CreateScheduleAndPost_EditIt_AndReadPersistedResult()
	{
		var client = fixture.AuthClient;
		var helper = new CreateHelper(client);
		var schedule = await helper.CreateSchedule();
		await helper.CreateDay(schedule, DateTime.UtcNow.DayOfWeek);
		var message = await helper.CreateMessage(schedule);
		var text = "Updated post " + Guid.NewGuid().ToString("N");
		using var edit = new MultipartFormDataContent
		{
			{ new StringContent(schedule.ToString()), "ScheduleId" },
			{ new StringContent(DateTimeOffset.UtcNow.AddDays(2).ToString("O", CultureInfo.InvariantCulture)), "TimePosting" },
			{ new StringContent(text), "TextMessage" }
		};

		var updated = await client.PutAsync($"/api/v1/message/{message}", edit);
		var read = await client.GetAsync($"/api/v1/message/{message}");
		var list = await client.GetFromJsonAsync<JsonElement>($"/api/v1/message?scheduleId={schedule}");

		Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
		Assert.Equal(HttpStatusCode.OK, read.StatusCode);
		Assert.Contains(text, await read.Content.ReadAsStringAsync());
		Assert.Contains(list.GetProperty("data").EnumerateArray(), item => item.GetProperty("id").GetGuid() == message);
	}

	[Fact]
	public async Task KeepCreatedPostsAndSchedulesPrivateToTheirOwner()
	{
		var helper = new CreateHelper(fixture.AuthClient);
		var schedule = await helper.CreateSchedule();
		var message = await helper.CreateMessage(schedule);
		using var foreign = fixture.GetClient(fixture.GenerateTestToken(Guid.NewGuid()));

		var foreignMessage = await foreign.GetAsync($"/api/v1/message/{message}");
		var foreignSchedule = await foreign.GetAsync($"/api/v1/schedule/{schedule}");
		var ownerMessage = await fixture.AuthClient.GetAsync($"/api/v1/message/{message}");

		Assert.Equal(HttpStatusCode.NotFound, foreignMessage.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, foreignSchedule.StatusCode);
		Assert.Equal(HttpStatusCode.OK, ownerMessage.StatusCode);
	}

	[Fact]
	public async Task RegisterSignInAndRotateRefreshToken_RejectingItsReuse()
	{
		using var client = fixture.CreateClient();
		var credentials = new { login = "e2e_" + Guid.NewGuid().ToString("N")[..16], password = "test-password-123" };
		var registration = await client.PostAsJsonAsync("/api/v1/account/sign-on", credentials);
		Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
		var signIn = await client.PostAsJsonAsync("/api/v1/account/sign-in", credentials);
		Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);
		var tokens = await signIn.Content.ReadFromJsonAsync<JsonElement>();
		var oldRefresh = tokens.GetProperty("refreshToken").GetString();

		var rotated = await client.PostAsJsonAsync("/api/v1/account/refresh-token", new { refreshToken = oldRefresh });
		var reused = await client.PostAsJsonAsync("/api/v1/account/refresh-token", new { refreshToken = oldRefresh });

		Assert.Equal(HttpStatusCode.OK, rotated.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, reused.StatusCode);
		var updated = await rotated.Content.ReadFromJsonAsync<JsonElement>();
		Assert.NotEqual(oldRefresh, updated.GetProperty("refreshToken").GetString());
		Assert.False(string.IsNullOrEmpty(updated.GetProperty("accessToken").GetString()));
	}
}
