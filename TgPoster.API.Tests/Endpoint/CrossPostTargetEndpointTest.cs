using System.Net;
using Shared.Enums;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.CrossPostTargets.CreateCrossPostTarget;
using TgPoster.API.Domain.UseCases.CrossPostTargets.ListCrossPostTargets;
using TgPoster.API.Domain.UseCases.CrossPostTargets.PreviewCrossPost;
using TgPoster.API.Models;
using TgPoster.API.Tests.Helper;
using TgPoster.Storage.Data;

namespace TgPoster.API.Tests.Endpoint;

public class CrossPostTargetEndpointTest(EndpointTestFixture fixture) : IClassFixture<EndpointTestFixture>
{
	private static readonly string Url = Routes.CrossPostTarget.Root
		.Replace("{scheduleId:guid}", GlobalConst.Worked.ScheduleId.ToString());

	private readonly HttpClient client = fixture.AuthClient;
	private readonly PosterContext context = fixture.GetDbContext();

	private CreateCrossPostTargetRequest CreateRequest(Guid accountId, CrossPostLinkTarget linkTarget = CrossPostLinkTarget.Post,
		string? customLink = null) => new()
	{
		SocialAccountId = accountId,
		Format = CrossPostFormat.Teaser,
		LinkTarget = linkTarget,
		CustomLink = customLink,
		CallToAction = null,
		IncludeMedia = true,
		IncludeParsed = false,
		DelayMinutes = 0
	};

	[Fact]
	public async Task Create_ThenList_ReturnsTarget()
	{
		var account = new SocialAccountBuilder(context).WithUserId(GlobalConst.Worked.UserId).Create();

		var created = await client.PostAsync<CreateCrossPostTargetResponse>(Url, CreateRequest(account.Id));

		created.Id.ShouldNotBe(Guid.Empty);

		var list = await client.GetAsync<List<CrossPostTargetResponse>>(Url);

		list.ShouldContain(x => x.Id == created.Id && x.SocialAccountId == account.Id);
	}

	[Fact]
	public async Task Update_ReturnsNoContent()
	{
		var account = new SocialAccountBuilder(context).WithUserId(GlobalConst.Worked.UserId).Create();
		var created = await client.PostAsync<CreateCrossPostTargetResponse>(Url, CreateRequest(account.Id));

		var request = new UpdateCrossPostTargetRequest
		{
			IsActive = false,
			Format = CrossPostFormat.Full,
			LinkTarget = CrossPostLinkTarget.Channel,
			IncludeMedia = false,
			IncludeParsed = true,
			DelayMinutes = 30
		};

		var response = await client.PutAsync($"{Url}/{created.Id}", request.ToStringContent());

		response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
	}

	[Fact]
	public async Task Delete_ReturnsNoContent()
	{
		var account = new SocialAccountBuilder(context).WithUserId(GlobalConst.Worked.UserId).Create();
		var created = await client.PostAsync<CreateCrossPostTargetResponse>(Url, CreateRequest(account.Id));

		var response = await client.DeleteAsync($"{Url}/{created.Id}");

		response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
	}

	[Fact]
	public async Task Preview_ReturnsOk()
	{
		var account = new SocialAccountBuilder(context).WithUserId(GlobalConst.Worked.UserId).Create();
		await client.PostAsync<CreateCrossPostTargetResponse>(Url, CreateRequest(account.Id));

		var request = new PreviewCrossPostRequest { Text = "Текст предпросмотра" };

		var response = await client.PostAsync<List<CrossPostPreviewResponse>>(Url + "/preview", request);

		response.ShouldNotBeEmpty();
	}

	[Fact]
	public async Task List_AnotherUserSchedule_ReturnsNotFound()
	{
		var anotherSchedule = Guid.NewGuid();

		var response = await client.GetAsync($"{Url.Replace(GlobalConst.Worked.ScheduleId.ToString(), anotherSchedule.ToString())}");

		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task Create_WithCustomLinkTargetWithoutLink_ReturnsBadRequest()
	{
		var account = new SocialAccountBuilder(context).WithUserId(GlobalConst.Worked.UserId).Create();

		var request = CreateRequest(account.Id, CrossPostLinkTarget.Custom);

		var response = await client.PostAsync(Url, request.ToStringContent());

		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}
}
