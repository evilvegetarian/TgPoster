using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Security.Interfaces;
using Security.Models;
using Shouldly;
using TgPoster.API.Common;

namespace TgPoster.API.Tests.Endpoint;

public class UnauthorizedAccessTests : IClassFixture<EndpointTestFixture>
{
	private readonly HttpClient _client;

	public UnauthorizedAccessTests(EndpointTestFixture fixture)
	{
		var customFactory = fixture.WithWebHostBuilder(builder =>
		{
			builder.ConfigureServices(services =>
			{
				var descriptor = services.FirstOrDefault(d =>
					d.ServiceType == typeof(IIdentityProvider));

				if (descriptor != null)
				{
					services.Remove(descriptor);
				}

				services.AddScoped<IIdentityProvider, DummyIdentityProvider>();
			});
		});
		_client = customFactory.CreateClient();
	}

	[Theory]
	[InlineData(Routes.Day.Root, "GET")]
	[InlineData(Routes.Day.Root, "POST")]
	[InlineData(Routes.Message.Root, "GET")]
	//[InlineData(Routes.Message.Root, "POST")]
	[InlineData(Routes.Schedule.Root, "GET")]
	[InlineData(Routes.Schedule.Root, "POST")]
	[InlineData(Routes.TelegramBot.Root, "GET")]
	[InlineData(Routes.TelegramBot.Root, "POST")]
	[InlineData(Routes.ParseChannel.Root, "POST")]
	[InlineData(Routes.ParseChannel.Root, "GET")]
	public async Task ProtectedEndpoints_ShouldReturnUnauthorized(string url, string method)
	{
		HttpResponseMessage response;
		if (method == "GET")
		{
			response = await _client.GetAsync(url);
		}
		else if (method == "POST")
		{
			response = await _client.PostAsync(url, JsonContent.Create(new { }));
		}
		else
		{
			throw new NotSupportedException();
		}

		response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
	}
}

public class DummyIdentityProvider : IIdentityProvider
{
	public Identity Current { get; private set; } = Identity.Anonymous;

	public void Set(Identity identity)
	{
		Current = identity ?? throw new ArgumentNullException(nameof(identity));
	}
}