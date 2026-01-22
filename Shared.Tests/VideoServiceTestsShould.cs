using OpenCvSharp;
using Shared.Video;
using Shouldly;

namespace Shared.Tests;

public sealed class VideoServiceTestsShould
{
	private readonly string filePath = Directory
		.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFiles"))
		.First(x => x.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase));

	private readonly VideoService sut = new();

	[Fact]
	public async Task ExtractScreenshots_WithValidVideo_ReturnsExpectedNumberOfScreenshots()
	{
		var fileBytes = await File.ReadAllBytesAsync(filePath);
		using var videoStream = new MemoryStream(fileBytes);
		var screenshotCount = 3;

		var screenshots = await sut.ExtractScreenshotsAsync(videoStream, screenshotCount);

		screenshots.ShouldNotBeNull();
		screenshots.Count.ShouldBe(screenshotCount);

		foreach (var imageBytes in screenshots.Select(screenshot => screenshot.ToArray()))
		{
			using var img = Cv2.ImDecode(imageBytes, ImreadModes.Color);
			img.Empty().ShouldBeFalse("Изображение не должно быть пустым.");
		}
	}

	[Fact]
	public async Task ExtractScreenshots_WithOutputWidth_ResizesScreenshots()
	{
		var fileBytes = await File.ReadAllBytesAsync(filePath);
		using var videoStream = new MemoryStream(fileBytes);
		var screenshotCount = 2;
		var outputWidth = 320;

		var screenshots = await sut.ExtractScreenshotsAsync(videoStream, screenshotCount, outputWidth);

		screenshots.ShouldNotBeNull();
		screenshots.Count.ShouldBe(screenshotCount);

		foreach (var screenshot in screenshots)
		{
			var imageBytes = screenshot.ToArray();
			using var img = Cv2.ImDecode(imageBytes, ImreadModes.Color);
			img.Empty().ShouldBeFalse();
			img.Width.ShouldBe(outputWidth);
		}
	}

	[Fact]
	public async Task ExtractScreenshots_NullStream_ThrowsArgumentNullException()
	{
		await Should.ThrowAsync<ArgumentNullException>(() => sut.ExtractScreenshotsAsync(null!, 1)
		);
	}

	[Fact]
	public async Task ExtractScreenshots_InvalidScreenshotCount_ThrowsArgumentException()
	{
		using var stream = new MemoryStream([1, 2, 3]);

		await Should.ThrowAsync<ArgumentException>(() => sut.ExtractScreenshotsAsync(stream, 0)
		);
	}

	[Fact]
	public async Task ExtractScreenshots_InvalidVideoStream_ThrowsInvalidOperationException()
	{
		using var invalidVideoStream = new MemoryStream([0, 1, 2, 3, 4, 5, 6, 7, 8, 9]);

		var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
			sut.ExtractScreenshotsAsync(invalidVideoStream, 1)
		);

		exception.Message.ShouldBe("Не удалось обработать видео с помощью FFMpeg.");
	}
}