using System.Net;
using Microsoft.Extensions.Configuration;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.OpenRouterSetting.CreateOpenRouterSetting;
using TgPoster.API.Domain.UseCases.OpenRouterSetting.GetOpenRouterSetting;
using TgPoster.API.Models;
using TgPoster.Endpoint.Tests.Helper;

namespace TgPoster.Endpoint.Tests.Endpoint;

public class OpenRouterSettingControllerTest(EndpointTestFixture fixture)
	: IClassFixture<EndpointTestFixture>
{
	private readonly HttpClient client = fixture.AuthClient;
	private readonly string Url = Routes.OpenRouterSetting.Root;
	private readonly string? Token = fixture.Token;

	[Fact]
	public async Task CreateOpenRouterSetting_WithNonValidModelData_ReturnsBadRequest()
	{
		var request = new CreateOpenRouterSettingRequest
		{
			Model = "deepseek/dsd",
			Token = Token
		};
		var createResponse = await client.PostAsync(Url, request.ToStringContent());
		createResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task CreateOpenRouterSetting_WithNonValidTokenData_ReturnsBadRequest()
	{
		var request = new CreateOpenRouterSettingRequest
		{
			Model = GlobalConst.Worked.Model,
			Token = "notvalidtoken"
		};
		var createResponse = await client.PostAsync(Url, request.ToStringContent());
		createResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task CreateOpenRouterSetting_WithValidTokenData_ReturnsCreated()
	{
		var request = new CreateOpenRouterSettingRequest
		{
			Model = GlobalConst.Worked.Model,
			Token = Token
		};
		var createResponse = await client.PostAsync(Url, request.ToStringContent());
		createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
		var setting = await createResponse.ToObject<CreateOpenRouterSettingResponse>();
		
		var getResponse = await client.GetAsync<GetOpenRouterSettingResponse>(Url + "/" + setting.Id);
		getResponse.Model.ShouldBe(request.Model);
	}
}