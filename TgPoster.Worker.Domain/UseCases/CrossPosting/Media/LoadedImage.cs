namespace TgPoster.Worker.Domain.UseCases.CrossPosting.Media;

/// <summary>
///     Загруженное и обработанное изображение в формате JPEG
/// </summary>
internal sealed record LoadedImage(byte[] Data, int Width, int Height);
