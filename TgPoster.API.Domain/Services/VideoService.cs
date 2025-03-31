using OpenCvSharp;

namespace TgPoster.API.Domain.Services;

internal sealed class VideoService
{
    public List<MemoryStream> ExtractScreenshots(MemoryStream videoStream, int screenshotCount, int outputWidth = 0)
    {
        if (videoStream == null)
            throw new ArgumentNullException(nameof(videoStream));
        if (screenshotCount < 1)
            throw new ArgumentException("Количество скриншотов должно быть не меньше 1", nameof(screenshotCount));

        var tempVideoPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".mp4");
        File.WriteAllBytes(tempVideoPath, videoStream.ToArray());

        var screenshots = new List<MemoryStream>();

        VideoCapture capture = null;
        try
        {
            capture = new VideoCapture(tempVideoPath);
            if (!capture.IsOpened())
                throw new ArgumentException("Не удалось открыть видео файл");

            // Получаем ключевые параметры видео.
            var fps = capture.Fps;
            var frameCount = capture.FrameCount;
            if (frameCount <= 0)
                throw new ArgumentException("Не удалось определить количество кадров");

            // Определяем длительность видео в секундах.
            var duration = frameCount / fps;

            // Для равномерного выбора кадров (без крайних), делим видео на screenshotCount+1 частей.
            // Вычисляем номера кадров для извлечения: для каждого скриншота определяем время, переводим в номер кадра.
            for (var i = 1; i <= screenshotCount; i++)
            {
                var snapshotTime = duration * i / (screenshotCount + 1); // в секундах
                var targetFrame = (int)(snapshotTime * fps);

                capture.Set(VideoCaptureProperties.PosFrames, targetFrame);

                using var frame = new Mat();
                if (!capture.Read(frame) || frame.Empty())
                    throw new ArgumentException($"Не удалось считать кадр под номером {targetFrame}");

                if (outputWidth > 0)
                {
                    var newWidth = outputWidth;
                    var newHeight = (int)(frame.Height * (outputWidth / (double)frame.Width));
                    Cv2.Resize(frame, frame, new Size(newWidth, newHeight));
                }

                Cv2.ImEncode(".jpg", frame, out var imageBytes);

                var screenshotStream = new MemoryStream(imageBytes);
                screenshots.Add(screenshotStream);
            }

            return screenshots;
        }
        finally
        {
            capture?.Release();
            if (File.Exists(tempVideoPath))
                File.Delete(tempVideoPath);
        }
    }
}