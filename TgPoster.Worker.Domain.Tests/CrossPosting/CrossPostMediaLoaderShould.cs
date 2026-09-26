using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using TgPoster.Worker.Domain.UseCases.CrossPosting;
using TgPoster.Worker.Domain.UseCases.CrossPosting.Media;

namespace TgPoster.Worker.Domain.Tests.CrossPosting;

public class CrossPostMediaLoaderShould
{
	private readonly Mock<ITelegramFileDownloader> downloader;
	private readonly CrossPostMediaLoader sut;

	public CrossPostMediaLoaderShould()
	{
		downloader = new Mock<ITelegramFileDownloader>();
		sut = new CrossPostMediaLoader(downloader.Object, NullLogger<CrossPostMediaLoader>.Instance);
	}

	[Fact]
	public async Task DownloadVideoThumbnail_WhenAvailable()
	{
		downloader
			.Setup(d => d.DownloadAsync("token", "thumb1", It.IsAny<CancellationToken>()))
			.ReturnsAsync(CreateJpegBytes(100, 100));
		var files = new List<CrossPostFileDto>
		{
			new()
			{
				TgFileId = "video1",
				ContentType = "video/mp4",
				Order = 1,
				ThumbnailTgFileId = "thumb1"
			}
		};

		var result = await sut.LoadAsync("token", files, CreateProfile(4), CancellationToken.None);

		result.Count.ShouldBe(1);
		downloader.Verify(d => d.DownloadAsync("token", "thumb1", It.IsAny<CancellationToken>()), Times.Once);
		downloader.Verify(d => d.DownloadAsync("token", "video1", It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task SkipVideo_WhenThumbnailMissing()
	{
		var files = new List<CrossPostFileDto>
		{
			new()
			{
				TgFileId = "video1",
				ContentType = "video/mp4",
				Order = 1,
				ThumbnailTgFileId = null
			}
		};

		var result = await sut.LoadAsync("token", files, CreateProfile(4), CancellationToken.None);

		result.ShouldBeEmpty();
		downloader.Verify(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task StopAtMaxImages_AndRespectOrder()
	{
		var jpeg = CreateJpegBytes(100, 100);
		downloader
			.Setup(d => d.DownloadAsync("token", It.IsIn("file1", "file2", "file3", "file4"), It.IsAny<CancellationToken>()))
			.ReturnsAsync(jpeg);

		var files = Enumerable.Range(1, 6)
			.Select(i => new CrossPostFileDto
			{
				TgFileId = $"file{i}",
				ContentType = "image/jpeg",
				Order = i
			})
			.ToList();

		var result = await sut.LoadAsync("token", files, CreateProfile(4), CancellationToken.None);

		result.Count.ShouldBe(4);
		downloader.Verify(
			d => d.DownloadAsync("token", "file5", It.IsAny<CancellationToken>()),
			Times.Never);
		downloader.Verify(
			d => d.DownloadAsync("token", "file6", It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task Continue_WhenOneDownloadFails()
	{
		var jpeg = CreateJpegBytes(100, 100);
		downloader
			.Setup(d => d.DownloadAsync("token", "file1", It.IsAny<CancellationToken>()))
			.ReturnsAsync(jpeg);
		downloader
			.Setup(d => d.DownloadAsync("token", "file2", It.IsAny<CancellationToken>()))
			.ReturnsAsync((byte[]?)null);
		downloader
			.Setup(d => d.DownloadAsync("token", "file3", It.IsAny<CancellationToken>()))
			.ReturnsAsync(jpeg);

		var files = new List<CrossPostFileDto>
		{
			new() { TgFileId = "file1", ContentType = "image/jpeg", Order = 1 },
			new() { TgFileId = "file2", ContentType = "image/jpeg", Order = 2 },
			new() { TgFileId = "file3", ContentType = "image/jpeg", Order = 3 }
		};

		var result = await sut.LoadAsync("token", files, CreateProfile(4), CancellationToken.None);

		result.Count.ShouldBe(2);
	}

	[Fact]
	public async Task SkipUnknownContentType()
	{
		var files = new List<CrossPostFileDto>
		{
			new()
			{
				TgFileId = "doc1",
				ContentType = "application/pdf",
				Order = 1
			}
		};

		var result = await sut.LoadAsync("token", files, CreateProfile(4), CancellationToken.None);

		result.ShouldBeEmpty();
	}

	private static MediaProfile CreateProfile(int maxImages)
	{
		return new MediaProfile(maxImages, 8_000_000, null, null);
	}

	private static byte[] CreateJpegBytes(int width, int height)
	{
		using var image = new Image<Rgba32>(width, height);
		using var stream = new MemoryStream();
		image.SaveAsJpeg(stream);
		return stream.ToArray();
	}
}
