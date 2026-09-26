using System.Net;
using NSubstitute;
using Shared.Enums;
using Shared.Social.Bluesky;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.SocialAccounts.ConnectBluesky;
using TgPoster.API.Domain.UseCases.SocialAccounts.ListSocialAccounts;
using TgPoster.API.Models;
using TgPoster.API.Tests.Helper;
using TgPoster.Storage.Data;

namespace TgPoster.API.Tests.Endpoint;

public class SocialAccountEndpointTest(EndpointTestFixture fixture) : IClassFixture<EndpointTestFixture>
{
	private const string Url = Routes.SocialAccount.Root;
	private readonly HttpClient client = fixture.AuthClient;
	private readonly PosterContext context = fixture.GetDbContext();

	[Fact]
	public async Task Get_List_ReturnsOk()
	{
		new SocialAccountBuilder(context).WithUserId(GlobalConst.Worked.UserId).Create();

		var response = await client.GetAsync<List<SocialAccountResponse>>(Url);

		response.Count.ShouldBeGreaterThan(0);
	}

	[Fact]
	public async Task ConnectBluesky_WithValidCredentials_ReturnsCreatedAndAppearsInList()
	{
		var handle = "test.bsky.social";
		fixture.BlueskyClient.CreateSessionAsync(
				Arg.Any<string>(),
				Arg.Any<string>(),
				Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(BlueskyResult<BlueskySession>.Ok(
				new BlueskySession("did:plc:abc", handle, "jwt", "refresh", "pds"))));

		var request = new ConnectBlueskyRequest { Handle = handle, AppPassword = "abcd-efgh-ijkl-mnop" };
		var response = await client.PostAsync<ConnectSocialAccountResponse>(Url + "/bluesky", request);

		response.Id.ShouldNotBe(Guid.Empty);

		var list = await client.GetAsync<List<SocialAccountResponse>>(Url);
		list.ShouldContain(x => x.Name == handle && x.Platform == SocialPlatform.Bluesky);
	}

	[Fact]
	public async Task ConnectBluesky_WithInvalidCredentials_ReturnsBadRequest()
	{
		fixture.BlueskyClient.CreateSessionAsync(
				Arg.Any<string>(),
				Arg.Any<string>(),
				Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(BlueskyResult<BlueskySession>.Fail(BlueskyErrorKind.Auth, "bad credentials")));

		var request = new ConnectBlueskyRequest { Handle = "test.bsky.social", AppPassword = "wrong-pass" };
		var response = await client.PostAsync(Url + "/bluesky", request.ToStringContent());

		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task Delete_AnotherUserAccount_ReturnsNotFound()
	{
		var accountId = new SocialAccountBuilder(context).WithUserId(GlobalConst.Worked.UserId).Create().Id;
		var anotherUserClient = fixture.GetClient(fixture.GenerateTestToken(GlobalConst.UserIdEmpty));

		var response = await anotherUserClient.DeleteAsync($"{Url}/{accountId}");

		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task Delete_NonExistent_ReturnsNotFound()
	{
		var response = await client.DeleteAsync($"{Url}/{Guid.NewGuid()}");

		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}
}
