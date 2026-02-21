using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Security.IdentityServices;
using Shouldly;
using TgPoster.API.Common;

namespace TgPoster.API.Tests.Endpoint;

public sealed class UnauthorizedAccessTests(EndpointTestFixture fixture) : IClassFixture<EndpointTestFixture>
{
	private readonly HttpClient client = CreateUnauthenticatedClient(fixture);

	[Theory]
	[InlineData(Routes.Day.Root, "GET")]
	[InlineData(Routes.Day.Root, "POST")]
	[InlineData(Routes.Message.Root, "GET")]
	[InlineData(Routes.Schedule.Root, "GET")]
	[InlineData(Routes.Schedule.Root, "POST")]
	[InlineData(Routes.TelegramBot.Root, "GET")]
	[InlineData(Routes.TelegramBot.Root, "POST")]
	[InlineData(Routes.ParseChannel.Root, "POST")]
	[InlineData(Routes.ParseChannel.Root, "GET")]
	[InlineData(Routes.Repost.CreateSettings, "GET")]
	[InlineData(Routes.Repost.CreateSettings, "POST")]
	[InlineData(Routes.CommentRepost.Root, "GET")]
	[InlineData(Routes.CommentRepost.Root, "POST")]
	[InlineData(Routes.YouTubeAccount.Root, "GET")]
	[InlineData(Routes.YouTubeAccount.Root, "POST")]
	[InlineData(Routes.PromptSetting.Root, "GET")]
	[InlineData(Routes.PromptSetting.Root, "POST")]
	[InlineData(Routes.OpenRouterSetting.Root, "GET")]
	[InlineData(Routes.OpenRouterSetting.Root, "POST")]
	[InlineData(Routes.TelegramSession.Root, "GET")]
	[InlineData(Routes.TelegramSession.Root, "POST")]
	public async Task ProtectedEndpoints_ShouldReturnUnauthorized(string url, string method)
	{
		var response = method switch
		{
			"GET" => await client.GetAsync(url),
			"POST" => await client.PostAsync(url, JsonContent.Create(new { })),
			"PUT" => await client.PutAsync(url, JsonContent.Create(new { })),
			"DELETE" => await client.DeleteAsync(url),
			_ => throw new NotSupportedException($"HTTP method '{method}' is not supported")
		};

		response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
	}

	private static HttpClient CreateUnauthenticatedClient(EndpointTestFixture fixture)
	{
		var customFactory = fixture.WithWebHostBuilder(builder =>
		{
			builder.ConfigureServices(services =>
			{
				var descriptor = services.FirstOrDefault(d =>
					d.ServiceType == typeof(IIdentityProvider));

				if (descriptor != null)
					services.Remove(descriptor);

				services.AddScoped<IIdentityProvider, DummyIdentityProvider>();
			});
		});
		return customFactory.CreateClient();
	}
}

internal sealed class DummyIdentityProvider : IIdentityProvider
{
	public Identity Current { get; private set; } = Identity.Anonymous;

	public void Set(Identity identity)
	{
		Current = identity ?? throw new ArgumentNullException(nameof(identity));
	}
}
