using System.Net;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.Endpoint.Tests.Helper;

namespace TgPoster.Endpoint.Tests.Endpoint;

public class FileEndpointTest(EndpointTestFixture fixture) : IClassFixture<EndpointTestFixture>
{
	private const string Url = Routes.File.Root;
	private readonly HttpClient client = fixture.AuthClient;

	[Fact]
	public async Task GetFile_WithValidId_ShouldReturnFile()
	{
		var response = await client.GetAsync($"{Url}/{GlobalConst.FileId}");
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		response.Content.Headers.ContentType?.MediaType.ShouldBe("image/jpeg");
		var content = await response.Content.ReadAsByteArrayAsync();
		content.Length.ShouldBeGreaterThan(0);
	}

	[Fact]
	public async Task GetFile_WithNonExistentId_ShouldReturnNotFound()
	{
		var nonExistentId = Guid.NewGuid();
		var response = await client.GetAsync($"{Url}/{nonExistentId}");
		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}
}