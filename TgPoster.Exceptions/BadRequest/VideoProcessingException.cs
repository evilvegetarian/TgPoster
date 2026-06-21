using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
///     Исключение, выбрасываемое при ошибках обработки видео с помощью FFMpeg
/// </summary>
public sealed class VideoProcessingException : DomainException
{
	public VideoProcessingException(string message, Exception? innerException = null)
		: base($"Не удалось обработать видео: {message}")
	{
		if (innerException != null)
		{
			InnerException = innerException;
		}
	}

	public new Exception? InnerException { get; init; }
}