using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Tests.Helper;

namespace TgPoster.API.Tests.Endpoint;

public sealed class DiscoverEndpointShould(EndpointTestFixture fixture) : IClassFixture<EndpointTestFixture>
{
	[Theory]
	[InlineData("")]
	[InlineData("/categories")]
	[InlineData("/status")]
	[InlineData("/stats")]
	[InlineData("/parse-history")]
	[InlineData("/settings")]
	[InlineData("/classification/stats")]
	[InlineData("/classification/history")]
	[InlineData("/classification/status")]
	[InlineData("/classification/settings")]
	public async Task ReadCatalogStatisticsHistoryAndSettings_WithoutTelegramRequests(string suffix)
	{
		var response = await fixture.AuthClient.GetAsync(Routes.Discover.Root + suffix);

		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		var json = await response.Content.ReadFromJsonAsync<JsonElement>();
		json.ValueKind.ShouldBeOneOf(JsonValueKind.Object, JsonValueKind.Array);
		if (suffix is "" or "/parse-history" or "/classification/history")
		{
			json.GetProperty("data").GetArrayLength().ShouldBe(0);
			json.GetProperty("totalCount").GetInt32().ShouldBe(0);
		}
	}

	[Theory]
	[InlineData("/settings")]
	[InlineData("/classification/settings")]
	public async Task SaveSettings_WithOwnedSessions_AndRejectForeignSessions(string suffix)
	{
		var url = Routes.Discover.Root + suffix;
		var settings = (await fixture.AuthClient.GetFromJsonAsync<JsonObject>(url))!;
		settings["isEnabled"] = false;
		settings["intervalMinutes"] = 13;
		settings["telegramSessionIds"] = new JsonArray(JsonValue.Create(GlobalConst.Worked.TelegramSessionId));

		var saved = await fixture.AuthClient.PutAsJsonAsync(url, settings);

		saved.StatusCode.ShouldBe(HttpStatusCode.NoContent);
		var stored = (await fixture.AuthClient.GetFromJsonAsync<JsonObject>(url))!;
		stored["isEnabled"]!.GetValue<bool>().ShouldBeFalse();
		stored["intervalMinutes"]!.GetValue<int>().ShouldBe(13);
		settings["telegramSessionIds"] = new JsonArray(JsonValue.Create(Guid.NewGuid()));
		(await fixture.AuthClient.PutAsJsonAsync(url, settings)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Theory]
	[InlineData("/stats?days=0")]
	[InlineData("/parse-history?from=2026-11-02&to=2026-11-01")]
	[InlineData("/classification/history?from=2026-11-02&to=2026-11-01")]
	public async Task RejectInvalidPeriods(string query)
	{
		(await fixture.AuthClient.GetAsync(Routes.Discover.Root + query)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Theory]
	[InlineData("message-statuses")]
	[InlineData("message-sort-fields")]
	[InlineData("sort-directions")]
	[InlineData("file-type")]
	public async Task ReturnOptions_WithUniqueValuesAndDisplayNames(string resource)
	{
		var response = await fixture.AuthClient.GetAsync("/api/options/" + resource);

		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		var values = (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();
		values.ShouldNotBeEmpty();
		values.Select(x => x.GetProperty("value").GetRawText()).Distinct().Count().ShouldBe(values.Count);
		values.ShouldAllBe(x => !string.IsNullOrWhiteSpace(x.GetProperty("name").GetString()));
	}
}
