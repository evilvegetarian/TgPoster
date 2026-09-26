using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace TgPoster.Worker.Domain.UseCases.CrossPosting.Media;

/// <summary>
///     Обработка изображений под требования площадки
/// </summary>
internal static class CrossPostImageProcessor
{
	private const int MinSideLength = 16;
	private const int JpegQualityFallback = 80;
	private const double ResizeFactor = 0.8;

	/// <summary>
	///     Перекодировать исходное изображение в JPEG с учётом ограничений площадки
	/// </summary>
	/// <param name="source"></param>
	/// <param name="profile"></param>
	/// <returns></returns>
	public static LoadedImage? Process(byte[] source, MediaProfile profile)
	{
		using var image = LoadImage(source);
		if (image is null)
		{
			return null;
		}

		PadToAspectRatio(image, profile);

		var qualities = new[] { 90, 80, 70, 60, 50 };
		foreach (var quality in qualities)
		{
			var bytes = SaveToJpeg(image, quality);
			if (bytes.Length <= profile.MaxImageBytes)
			{
				return new LoadedImage(bytes, image.Width, image.Height);
			}
		}

		var currentWidth = image.Width;
		var currentHeight = image.Height;

		while (true)
		{
			currentWidth = (int)(currentWidth * ResizeFactor);
			currentHeight = (int)(currentHeight * ResizeFactor);

			if (Math.Min(currentWidth, currentHeight) < MinSideLength)
			{
				return null;
			}

			image.Mutate(x => x.Resize(currentWidth, currentHeight));

			var bytes = SaveToJpeg(image, JpegQualityFallback);
			if (bytes.Length <= profile.MaxImageBytes)
			{
				return new LoadedImage(bytes, image.Width, image.Height);
			}
		}
	}

	private static Image? LoadImage(byte[] source)
	{
		try
		{
			return Image.Load(source);
		}
		catch (UnknownImageFormatException)
		{
			return null;
		}
		catch (InvalidImageContentException)
		{
			return null;
		}
	}

	private static void PadToAspectRatio(Image image, MediaProfile profile)
	{
		var width = image.Width;
		var height = image.Height;
		var aspect = (double)width / height;

		if (profile.MinAspect.HasValue && aspect < profile.MinAspect.Value)
		{
			var targetWidth = (int)Math.Ceiling(height * profile.MinAspect.Value);
			image.Mutate(x => x.Resize(new ResizeOptions
			{
				Size = new Size(targetWidth, height),
				Mode = ResizeMode.BoxPad,
				PadColor = Color.Black,
				Position = AnchorPositionMode.Center
			}));
		}
		else if (profile.MaxAspect.HasValue && aspect > profile.MaxAspect.Value)
		{
			var targetHeight = (int)Math.Ceiling(width / profile.MaxAspect.Value);
			image.Mutate(x => x.Resize(new ResizeOptions
			{
				Size = new Size(width, targetHeight),
				Mode = ResizeMode.BoxPad,
				PadColor = Color.Black,
				Position = AnchorPositionMode.Center
			}));
		}
	}

	private static byte[] SaveToJpeg(Image image, int quality)
	{
		using var stream = new MemoryStream();
		image.SaveAsJpeg(stream, new JpegEncoder { Quality = quality });
		return stream.ToArray();
	}
}
