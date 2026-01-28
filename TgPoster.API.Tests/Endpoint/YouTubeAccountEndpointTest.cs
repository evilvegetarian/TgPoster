using System.Net;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Tests.Helper;
using TgPoster.Storage.Data;

namespace TgPoster.API.Tests.Endpoint;

public class YouTubeAccountEndpointTest(EndpointTestFixture fixture) : IClassFixture<EndpointTestFixture>
{
	private const string Url = Routes.YouTubeAccount.Root;
	private readonly HttpClient client = fixture.AuthClient;
	private readonly PosterContext context = fixture.GetDbContext();

	[Fact]
	public async Task DeleteYouTubeAccount_WithValidId_ShouldReturnOk()
	{
		var youtubeAccountId = new YouTubeAccountBuilder(context).WithUserId(GlobalConst.Worked.UserId).Create().Id;
		var deleteResponse = await client.DeleteAsync($"{Url}/{youtubeAccountId}");

		deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
	}

	[Fact]
	public async Task DeleteYouTubeAccount_WithNonExistentId_ShouldReturnNotFound()
	{
		var nonExistentId = Guid.NewGuid();

		var deleteResponse = await client.DeleteAsync($"{Url}/{nonExistentId}");

		deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task DeleteYouTubeAccount_WithAnotherUserAccount_ShouldReturnNotFound()
	{
		var anotherUserClient = fixture.GetClient(fixture.GenerateTestToken(GlobalConst.UserIdEmpty));

		var deleteResponse = await anotherUserClient.DeleteAsync($"{Url}/{GlobalConst.YouTubeAccountId}");

		deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task DeleteYouTubeAccount_WhenDeleted_ShouldReturnNotFoundOnSecondAttempt()
	{
		var youtubeAccountId = new YouTubeAccountBuilder(context).WithUserId(GlobalConst.Worked.UserId).Create().Id;

		var firstDeleteResponse = await client.DeleteAsync($"{Url}/{youtubeAccountId}");
		firstDeleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

		var secondDeleteResponse = await client.DeleteAsync($"{Url}/{youtubeAccountId}");
		secondDeleteResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}
}