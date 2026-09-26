using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using Shouldly;
using Shared.Enums;
using TgPoster.Worker.Domain.UseCases.CrossPosting.Media;

namespace TgPoster.Worker.Domain.Tests.CrossPosting;

public class CrossPostImageProcessorShould
{
	[Fact]
	public void ConvertPngToJpeg()
	{
		var source = CreatePngBytes(100, 100);

		var result = CrossPostImageProcessor.Process(source, MediaProfile.For(SocialPlatform.Bluesky));

		result.ShouldNotBeNull();
		result.Width.ShouldBe(100);
		result.Height.ShouldBe(100);
		((int)result.Data[0]).ShouldBe(0xFF);
		((int)result.Data[1]).ShouldBe(0xD8);
	}

	[Fact]
	public void ReturnNull_ForInvalidBytes()
	{
		var source = new byte[] { 1, 2, 3, 4, 5 };

		var result = CrossPostImageProcessor.Process(source, MediaProfile.For(SocialPlatform.Bluesky));

		result.ShouldBeNull();
	}

	[Fact]
	public void PadImage_WhenTooWide()
	{
		var source = CreatePngBytes(1000, 200);
		var profile = new MediaProfile(10, 8_000_000, 0.8, 1.91);

		var result = CrossPostImageProcessor.Process(source, profile);

		result.ShouldNotBeNull();
		result.Width.ShouldBe(1000);
		(result.Width / (double)result.Height <= profile.MaxAspect!.Value).ShouldBeTrue();
	}

	[Fact]
	public void PadImage_WhenTooNarrow()
	{
		var source = CreatePngBytes(200, 1000);
		var profile = new MediaProfile(10, 8_000_000, 0.8, 1.91);

		var result = CrossPostImageProcessor.Process(source, profile);

		result.ShouldNotBeNull();
		(result.Width / (double)result.Height >= profile.MinAspect!.Value).ShouldBeTrue();
	}

	[Fact]
	public void CompressNoisyImage_BelowMaxBytes()
	{
		var source = CreateNoisyJpegBytes(2000, 2000);
		var profile = new MediaProfile(1, 100_000, null, null);

		var result = CrossPostImageProcessor.Process(source, profile);

		result.ShouldNotBeNull();
		((long)result.Data.Length).ShouldBeLessThanOrEqualTo(profile.MaxImageBytes);
	}

	[Fact]
	public void KeepAspectRatio_ForBluesky()
	{
		var source = CreatePngBytes(100, 200);
		var profile = MediaProfile.For(SocialPlatform.Bluesky);

		var result = CrossPostImageProcessor.Process(source, profile);

		result.ShouldNotBeNull();
		result.Width.ShouldBe(100);
		result.Height.ShouldBe(200);
	}

	private static byte[] CreatePngBytes(int width, int height)
	{
		using var image = new Image<Rgba32>(width, height);
		using var stream = new MemoryStream();
		image.SaveAsPng(stream);
		return stream.ToArray();
	}

	private static byte[] CreateNoisyJpegBytes(int width, int height)
	{
		using var image = new Image<Rgba32>(width, height);
		var random = new Random(42);

		for (var y = 0; y < height; y++)
		{
			for (var x = 0; x < width; x++)
			{
				image[x, y] = new Rgba32(
					(byte)random.Next(256),
					(byte)random.Next(256),
					(byte)random.Next(256));
			}
		}

		using var stream = new MemoryStream();
		image.SaveAsJpeg(stream);
		return stream.ToArray();
	}
}
