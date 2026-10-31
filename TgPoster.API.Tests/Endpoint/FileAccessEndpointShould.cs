using System.Net;
using System.Net.Http.Json;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;

namespace TgPoster.API.Tests.Endpoint;

public sealed class FileAccessEndpointShould(AuthorizationTestFixture fixture) : IClassFixture<AuthorizationTestFixture>
{
	[Fact]
	public async Task ReturnFile_WhenRequestedByOwner()
	{
		using var client = fixture.GetClient(AuthorizationTestFixture.OwnerId);

		var response = await client.GetAsync($"{Routes.File.Root}/{AuthorizationTestFixture.FileId}");

		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		response.Content.Headers.ContentType!.MediaType.ShouldBe("image/jpeg");
		(await response.Content.ReadAsByteArrayAsync()).ShouldBe(new byte[] { 0x89, 0x50, 0x4e, 0x47 });
	}

	[Theory]
	[InlineData("")]
	[InlineData("/upload-s3")]
	public async Task ReturnNotFound_WhenRequestedByAnotherUser(string suffix)
	{
		using var client = fixture.GetClient(Guid.NewGuid());

		var response = await client.GetAsync($"{Routes.File.Root}/{AuthorizationTestFixture.FileId}{suffix}");

		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task RedirectToStorage_WhenOwnerRequestsCachedFile()
	{
		using var client = fixture.GetClient(AuthorizationTestFixture.OwnerId);

		var response = await client.GetAsync($"{Routes.File.Root}/{AuthorizationTestFixture.FileId}/upload-s3");

		response.StatusCode.ShouldBe(HttpStatusCode.Found);
		response.Headers.Location!.AbsoluteUri.ShouldBe($"https://files.example.test/bucket/{AuthorizationTestFixture.FileId}");
	}

	[Fact]
	public async Task AllowBrowserMediaRequest_WhenOwnerObtainedSignedLink()
	{
		var link = await GetSignedLinkAsync();
		using var browser = fixture.GetClient();

		var response = await browser.GetAsync(link);

		response.StatusCode.ShouldBe(HttpStatusCode.Found);
	}

	[Fact]
	public async Task RejectSignedLink_WhenFileIdWasChanged()
	{
		var link = await GetSignedLinkAsync();
		link = link.Replace(AuthorizationTestFixture.FileId.ToString(), Guid.NewGuid().ToString());
		using var browser = fixture.GetClient();

		var response = await browser.GetAsync(link);

		response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task RejectSignedLink_WhenSignatureWasChanged()
	{
		var link = await GetSignedLinkAsync();
		using var browser = fixture.GetClient();

		var response = await browser.GetAsync(link + "invalid");

		response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task RejectSignedLink_WhenExpired()
	{
		var link = await GetSignedLinkAsync();
		fixture.Clock.Now = fixture.Clock.Now.AddMinutes(11);
		using var browser = fixture.GetClient();

		var response = await browser.GetAsync(link);

		response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task NotExposeSignedLink_WhenPostBelongsToAnotherUser()
	{
		using var client = fixture.GetClient(Guid.NewGuid());

		var response = await client.GetAsync($"{Routes.Message.Root}/{AuthorizationTestFixture.MessageId}");

		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	private async Task<string> GetSignedLinkAsync()
	{
		using var client = fixture.GetClient(AuthorizationTestFixture.OwnerId);
		var message = await client.GetFromJsonAsync<MessageResponse>(
			$"{Routes.Message.Root}/{AuthorizationTestFixture.MessageId}");
		message.ShouldNotBeNull();
		return message.Files.Single().Url!;
	}
}
