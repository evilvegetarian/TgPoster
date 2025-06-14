
using System.Drawing;
using FFMpegCore;

namespace Shared;

public sealed class VideoService
{
    // Рекомендуется настроить путь к ffmpeg глобально при старте приложения, 
    // если он не находится в PATH или рядом с .exe.
    // Пример: GlobalFFOptions.Configure(new FFOptions { FfmpegPath = @"C:\ffmpeg\bin\ffmpeg.exe" });

    /// <summary>
    /// Извлекает указанное количество скриншотов из видеопотока, равномерно распределяя их по длительности.
    /// </summary>
    /// <param name="videoStream">Видео в виде MemoryStream.</param>
    /// <param name="screenshotCount">Количество скриншотов для извлечения.</param>
    /// <param name="outputWidth">Желаемая ширина скриншотов. Высота будет подобрана с сохранением пропорций. 0 - исходный размер.</param>
    /// <returns>Список MemoryStream, содержащих изображения в формате JPG.</returns>
    public async Task<List<MemoryStream>> ExtractScreenshotsAsync(MemoryStream videoStream, int screenshotCount, int outputWidth = 0)
    {
        if (videoStream == null)
        {
            throw new ArgumentNullException(nameof(videoStream));
        }

        if (screenshotCount < 1)
        {
            throw new ArgumentException("Количество скриншотов должно быть не меньше 1.", nameof(screenshotCount));
        }

        // FFMpeg работает с файлами, поэтому нам все равно нужно сохранить поток на диск временно.
        var tempVideoPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".mp4");
        await File.WriteAllBytesAsync(tempVideoPath, videoStream.ToArray());

        var screenshots = new List<MemoryStream>();

        try
        {
            // Получаем информацию о видео (включая длительность и разрешение) с помощью ffprobe.
            // Это гораздо надежнее, чем расчет через FPS и количество кадров.
            var mediaInfo = await FFProbe.AnalyseAsync(tempVideoPath);
            var duration = mediaInfo.Duration;

            Size? size = null;
            if (outputWidth > 0 && mediaInfo.PrimaryVideoStream != null)
            {
                var originalWidth = mediaInfo.PrimaryVideoStream.Width;
                var originalHeight = mediaInfo.PrimaryVideoStream.Height;
                if (originalWidth > 0 && originalHeight > 0)
                {
                    var newHeight = (int)Math.Round(originalHeight * (outputWidth / (double)originalWidth));
                    size = new Size(outputWidth, newHeight);
                }
            }

            // Равномерно вычисляем моменты времени для создания скриншотов.
            for (var i = 1; i <= screenshotCount; i++)
            {
                var snapshotTime = TimeSpan.FromSeconds(duration.TotalSeconds * i / (screenshotCount + 1));
                
                // Создаем скриншот во временный файл изображения
                var tempScreenshotPath = Path.ChangeExtension(Path.GetTempFileName(), ".jpg");

                try
                {
                    // FFmpeg сам найдет ближайший кадр, изменит размер и сохранит в файл.
                    await FFMpeg.SnapshotAsync(tempVideoPath, tempScreenshotPath, size, snapshotTime);
                    
                    // Читаем результат из временного файла в MemoryStream
                    var imageBytes = await File.ReadAllBytesAsync(tempScreenshotPath);
                    var screenshotStream = new MemoryStream(imageBytes);
                    screenshots.Add(screenshotStream);
                }
                finally
                {
                    // Удаляем временный файл скриншота
                    if(File.Exists(tempScreenshotPath))
                    {
                        File.Delete(tempScreenshotPath);
                    }
                }
            }

            return screenshots;
        }
        catch (Exception ex)
        {
            // Здесь можно добавить логирование ошибки
            throw new InvalidOperationException("Не удалось обработать видео с помощью FFMpeg.", ex);
        }
        finally
        {
            // Обязательно удаляем временный видеофайл
            if (File.Exists(tempVideoPath))
            {
                File.Delete(tempVideoPath);
            }
        }
    }
}