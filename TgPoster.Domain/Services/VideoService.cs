using FFMpegCore;

namespace TgPoster.Domain.Services;

internal sealed class VideoService
{
    public List<MemoryStream> ExtractScreenshots(
        MemoryStream videoStream,
        int screenshotCount,
        int outputWidth = 0
    )
    {
        if (videoStream == null)
            throw new ArgumentNullException(nameof(videoStream));
        if (screenshotCount < 1)
            throw new ArgumentException("Количество скриншотов должно быть не меньше 1", nameof(screenshotCount));

        // Создаём временный файл для видео.
        string tempVideoPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".mp4");
        var screenshots = new List<MemoryStream>();

        try
        {
            // Сохраняем содержимое MemoryStream во временный файл.
            File.WriteAllBytes(tempVideoPath, videoStream.ToArray());

            // Получаем метаданные видео (например, продолжительность).
            var mediaInfo = FFProbe.Analyse(tempVideoPath);
            if (mediaInfo.Duration.TotalSeconds <= 0)
                throw new Exception("Не удалось определить продолжительность видео");

            // Для получения скриншотов не берём крайние кадры – делим видео на (screenshotCount+1) частей.
            for (int i = 1; i <= screenshotCount; i++)
            {
                // Вычисляем момент, в котором будет сделан скриншот.
                var snapshotTime = TimeSpan.FromSeconds(
                    (mediaInfo.Duration.TotalSeconds * i) / (screenshotCount + 1)
                );

                // Создаём временный файл для скриншота.
                string tempScreenshotPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".jpg");

                // Создаём скриншот с/без изменения размера.
                if (outputWidth > 0)
                    FFMpeg.Snapshot(tempVideoPath, tempScreenshotPath, null, snapshotTime, outputWidth);
                else
                    FFMpeg.Snapshot(tempVideoPath, tempScreenshotPath, null, snapshotTime);

                // Загружаем изображение из временного файла в MemoryStream.
                byte[] imageBytes = File.ReadAllBytes(tempScreenshotPath);
                var screenshotStream = new MemoryStream(imageBytes);
                screenshots.Add(screenshotStream);

                // Удаляем временный файл скриншота.
                if (File.Exists(tempScreenshotPath))
                    File.Delete(tempScreenshotPath);
            }

            return screenshots;
        }
        finally
        {
            // Удаляем временный файл видео.
            if (File.Exists(tempVideoPath))
                File.Delete(tempVideoPath);
        }
    }
}