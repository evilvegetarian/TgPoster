using Shouldly;

namespace Shared.Tests;

public sealed class YouTubeServiceShould
{
	private readonly YouTubeService sut = new();

	[Fact]
	public async Task UploadVideoAsync_WithNullAccount_ThrowsArgumentNullException()
	{
		using var stream = new MemoryStream([1, 2, 3]);

		await Should.ThrowAsync<ArgumentNullException>(() =>
			sut.UploadVideoAsync(null!, stream, "Title", "Description", "tag1,tag2", CancellationToken.None)
		);
	}

	[Fact]
	public async Task UploadVideoAsync_WithNullStream_ThrowsArgumentNullException()
	{
		var account = new YouTubeAccountDto
		{
			AccessToken = "test_access_token",
			RefreshToken = "test_refresh_token",
			ClientId = "test_client_id",
			ClientSecret = "test_client_secret"
		};

		await Should.ThrowAsync<ArgumentNullException>(() =>
			sut.UploadVideoAsync(account, null!, "Title", "Description", "tag1,tag2", CancellationToken.None)
		);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public async Task UploadVideoAsync_WithInvalidTitle_ThrowsArgumentException(string? invalidTitle)
	{
		var account = new YouTubeAccountDto
		{
			AccessToken = "test_access_token",
			RefreshToken = "test_refresh_token",
			ClientId = "test_client_id",
			ClientSecret = "test_client_secret"
		};
		using var stream = new MemoryStream([1, 2, 3]);

		await Should.ThrowAsync<ArgumentException>(() =>
			sut.UploadVideoAsync(account, stream, invalidTitle!, "Description", "tag1,tag2", CancellationToken.None)
		);
	}

	[Fact]
	public async Task UploadVideoAsync_WithEmptyTags_DoesNotThrow()
	{
		var account = new YouTubeAccountDto
		{
			AccessToken = "test_access_token",
			RefreshToken = "test_refresh_token",
			ClientId = "test_client_id",
			ClientSecret = "test_client_secret"
		};
		using var stream = new MemoryStream([1, 2, 3]);

		var exception = await Record.ExceptionAsync(() =>
			sut.UploadVideoAsync(account, stream, "Title", "Description", "", CancellationToken.None)
		);

		exception.ShouldNotBeOfType<ArgumentException>();
	}
}
