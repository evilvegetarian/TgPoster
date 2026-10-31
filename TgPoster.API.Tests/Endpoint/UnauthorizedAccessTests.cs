using System.Net;
using System.Net.Http.Json;
using Shouldly;
using TgPoster.API.Common;

namespace TgPoster.API.Tests.Endpoint;

public sealed class UnauthorizedAccessTests(AuthorizationTestFixture fixture) : IClassFixture<AuthorizationTestFixture>
{
	private readonly HttpClient client = fixture.GetClient();

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
	[InlineData(Routes.Dashboard.Root, "GET")]
	[InlineData(Routes.Discover.List, "GET")]
	[InlineData(Routes.Discover.Settings, "GET")]
	[InlineData(Routes.Discover.Settings, "PUT")]
	[InlineData(Routes.Discover.ClassificationSettings, "PUT")]
	[InlineData(Routes.Proxy.List, "GET")]
	[InlineData(Routes.Proxy.Create, "POST")]
	[InlineData(Routes.Proxy.Update, "PUT")]
	[InlineData(Routes.Proxy.Delete, "DELETE")]
	[InlineData(Routes.SocialAccount.List, "GET")]
	[InlineData(Routes.SocialAccount.ConnectBluesky, "POST")]
	[InlineData(Routes.SocialAccount.Delete, "DELETE")]
	[InlineData(Routes.CrossPostTarget.List, "GET")]
	[InlineData(Routes.CrossPostTarget.Create, "POST")]
	[InlineData(Routes.CrossPostTarget.Update, "PUT")]
	[InlineData(Routes.CrossPostTarget.Delete, "DELETE")]
	[InlineData(Routes.CrossPostTarget.Preview, "POST")]
	[InlineData(Routes.ChannelCleanup.Preview, "GET")]
	[InlineData(Routes.ChannelCleanup.Start, "POST")]
	[InlineData(Routes.ChannelCleanup.GetJob, "GET")]
	[InlineData(Routes.File.Get, "GET")]
	[InlineData(Routes.File.UploadToS3, "GET")]
	[InlineData(Routes.Schedule.Update, "PUT")]
	[InlineData(Routes.Schedule.UpdateStatus, "PATCH")]
	[InlineData(Routes.Schedule.Delete, "DELETE")]
	[InlineData(Routes.Message.Update, "PUT")]
	[InlineData(Routes.Message.ApproveMessages, "PATCH")]
	[InlineData(Routes.Message.Delete, "DELETE")]
	[InlineData(Routes.Message.DeleteFileMessage, "DELETE")]
	[InlineData(Routes.Message.DeleteFromChannel, "DELETE")]
	[InlineData(Routes.Message.Retry, "POST")]
	[InlineData(Routes.TelegramBot.Update, "PUT")]
	[InlineData(Routes.TelegramBot.Delete, "DELETE")]
	[InlineData(Routes.TelegramSession.Update, "PUT")]
	[InlineData(Routes.TelegramSession.Delete, "DELETE")]
	[InlineData(Routes.ParseChannel.Update, "PUT")]
	[InlineData(Routes.ParseChannel.Delete, "DELETE")]
	[InlineData(Routes.CommentRepost.Update, "PUT")]
	[InlineData(Routes.CommentRepost.Delete, "DELETE")]
	[InlineData(Routes.Repost.UpdateSettings, "PUT")]
	[InlineData(Routes.Repost.DeleteSettings, "DELETE")]
	[InlineData(Routes.YouTubeAccount.Delete, "DELETE")]
	public async Task ProtectedEndpoints_ShouldReturnUnauthorized(string url, string method)
	{
		foreach (var parameter in new[] { "id", "scheduleId", "sessionId", "jobId", "fileId" })
			url = url.Replace("{" + parameter + ":guid}", "11111111-1111-1111-1111-111111111111");
		using var request = new HttpRequestMessage(new HttpMethod(method), url);
		if (method is "POST" or "PUT" or "PATCH" or "DELETE")
			request.Content = method == "PUT" && url.StartsWith(Routes.Message.Root + "/")
				? new MultipartFormDataContent() : JsonContent.Create(new { });

		var response = await client.SendAsync(request);

		response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
	}

}
