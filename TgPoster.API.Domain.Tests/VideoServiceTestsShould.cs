using OpenCvSharp;
using Shared;
using Shouldly;

namespace TgPoster.API.Domain.Tests;

public sealed class VideoServiceTestsShould
{
    private readonly string filePath = Directory
        .GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFiles"))
        .First(x => x.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase));

    private readonly VideoService sut = new();

    [Fact]
    public void ExtractScreenshots_WithValidVideo_ReturnsExpectedNumberOfScreenshots()
    {
        var fileBytes = File.ReadAllBytes(filePath);
        using var videoStream = new MemoryStream(fileBytes);
        var screenshotCount = 2;

        var screenshots = sut.ExtractScreenshots(videoStream, screenshotCount);

        screenshots.ShouldNotBeNull();
        screenshots.Count.ShouldBe(screenshotCount);

        foreach (var imageBytes in screenshots.Select(screenshot => screenshot.ToArray()))
        {
            using var img = Cv2.ImDecode(imageBytes, ImreadModes.Color);
            img.Empty().ShouldBeFalse("Изображение не должно быть пустым");
        }
    }

    [Fact]
    public void ExtractScreenshots_WithOutputWidth_ResizesScreenshots()
    {
        var fileBytes = File.ReadAllBytes(filePath);
        using var videoStream = new MemoryStream(fileBytes);
        var screenshotCount = 5;
        var outputWidth = 300;
        var screenshots = sut.ExtractScreenshots(videoStream, screenshotCount, outputWidth);

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
    public void ExtractScreenshots_NullStream_ThrowsArgumentNullException()
    {
        var exception = Record.Exception(() => sut.ExtractScreenshots(null!, 1));
        exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact]
    public void ExtractScreenshots_InvalidScreenshotCount_ThrowsArgumentException()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        var exception = Record.Exception(() => sut.ExtractScreenshots(stream, 0));
        exception.ShouldBeOfType<ArgumentException>();
    }

    [Fact]
    public void ExtractScreenshots_InvalidVideoStream_ThrowsException()
    {
        using var invalidVideoStream = new MemoryStream([0, 1, 2, 3, 4, 5]);

        var exception = Should.Throw<ArgumentException>(() => sut.ExtractScreenshots(invalidVideoStream, 1));

        exception.ShouldNotBeNull();
        exception.Message.ShouldContain("Не удалось открыть видео файл");
    }
}