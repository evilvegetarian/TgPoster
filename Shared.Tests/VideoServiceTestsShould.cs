using OpenCvSharp;
using Shared.Video;
using Shouldly;
using TgPoster.Exceptions.BadRequest;

namespace Shared.Tests;

public sealed class VideoServiceTestsShould
{
	private readonly string filePath = Directory
		.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFiles"))
		.First(x => x.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase));

	private readonly VideoService sut = new();

	[Fact]
	public async Task ReadVideoDuration_AndRewindInput()
	{
		await using var input = new MemoryStream(await File.ReadAllBytesAsync(filePath));
		input.Position = 10;

		var duration = await sut.GetDurationAsync(input);

		duration.ShouldBeGreaterThan(TimeSpan.Zero);
		input.Position.ShouldBe(0);
		input.CanRead.ShouldBeTrue();
	}

	[Fact]
	public async Task TrimVideoToRequestedDuration_AndReturnReadableRewoundStream()
	{
		await using var input = new MemoryStream(await File.ReadAllBytesAsync(filePath));

		await using var output = await sut.TrimVideoAsync(input, TimeSpan.FromSeconds(1));
		var duration = await sut.GetDurationAsync(output);

		output.Length.ShouldBeGreaterThan(0);
		output.Position.ShouldBe(0);
		input.Position.ShouldBe(0);
		duration.ShouldBeGreaterThan(TimeSpan.Zero);
		duration.ShouldBeLessThanOrEqualTo(TimeSpan.FromSeconds(1.2));
	}

	[Fact]
	public async Task RejectCorruptVideo_WhenReadingDurationOrTrimming()
	{
		await using var input = new MemoryStream([1, 2, 3]);

		await Should.ThrowAsync<VideoProcessingException>(() => sut.GetDurationAsync(input));
		await Should.ThrowAsync<VideoProcessingException>(() => sut.TrimVideoAsync(input, TimeSpan.FromSeconds(1)));
	}

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
		await Should.ThrowAsync<InvalidVideoStreamException>(() => sut.ExtractScreenshotsAsync(null!, 1));
	}

	[Fact]
	public async Task ExtractScreenshots_InvalidScreenshotCount_ThrowsArgumentException()
	{
		using var stream = new MemoryStream([1, 2, 3]);

		await Should.ThrowAsync<InvalidScreenshotCountException>(() => sut.ExtractScreenshotsAsync(stream, 0)
		);
	}

	[Fact]
	public async Task ExtractScreenshots_InvalidVideoStream_ThrowsInvalidOperationException()
	{
		using var invalidVideoStream = new MemoryStream([0, 1, 2, 3, 4, 5, 6, 7, 8, 9]);

		await Should.ThrowAsync<VideoProcessingException>(() =>
			sut.ExtractScreenshotsAsync(invalidVideoStream, 1)
		);
	}
}
