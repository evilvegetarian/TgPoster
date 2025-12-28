using System.Drawing;
using FFMpegCore;
using FFMpegCore.Pipes;

namespace Shared;

public sealed class VideoService
{
	/// <summary>
	///     Извлекает указанное количество скриншотов из видеопотока, равномерно распределяя их по длительности.
	///     Эта версия оптимизирована для минимального использования памяти и дискового I/O.
	/// </summary>
	/// <param name="videoStream">Видео в виде MemoryStream. Поток будет прочитан, но не закрыт.</param>
	/// <param name="screenshotCount">Количество скриншотов для извлечения.</param>
	/// <param name="outputWidth">
	///     Желаемая ширина скриншотов. Высота будет подобрана с сохранением пропорций. 0 - исходный
	///     размер.
	/// </param>
	/// <returns>
	///     Список MemoryStream, содержащих изображения в формате JPG.
	///     Вызывающий код несет ответственность за освобождение (Dispose) этих потоков.
	/// </returns>
	public async Task<List<MemoryStream>> ExtractScreenshotsAsync(
		MemoryStream videoStream,
		int screenshotCount,
		int outputWidth = 0
	)
	{
		if (videoStream == null)
		{
			throw new ArgumentNullException(nameof(videoStream));
		}

		if (screenshotCount < 1)
		{
			throw new ArgumentException("Количество скриншотов должно быть не меньше 1.", nameof(screenshotCount));
		}

		var tempVideoPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".tmp");
		var screenshots = new List<MemoryStream>();

		try
		{
			// 1. Оптимизированная запись потока в файл без создания лишнего массива  памяти
			videoStream.Position = 0; // Убедимся, что читаем с начала
			await using (var fileStream =
			             new FileStream(tempVideoPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
			{
				await videoStream.CopyToAsync(fileStream);
			}

			// 2. Получаем информацию о видео. Этот шаг остается прежним.
			var mediaInfo = await FFProbe.AnalyseAsync(tempVideoPath);
			var duration = mediaInfo.Duration;

			Size? size = null;
			if (outputWidth > 0 && mediaInfo.PrimaryVideoStream != null)
			{
				var vs = mediaInfo.PrimaryVideoStream;
				if (vs.Width > 0 && vs.Height > 0)
				{
					var newHeight = (int)Math.Round(vs.Height * (outputWidth / (double)vs.Width));
					size = new Size(outputWidth, newHeight);
				}
			}

			for (var i = 1; i <= screenshotCount; i++)
			{
				var snapshotTime = TimeSpan.FromSeconds(duration.TotalSeconds * i / (screenshotCount + 1));

				// 3. Готовим MemoryStream, КУДА FFmpeg будет писать данные скриншота
				var outputStream = new MemoryStream();
				screenshots.Add(outputStream);

				// 4. Используем Pipe для вывода напрямую в наш MemoryStream
				var arguments = FFMpegArguments
					.FromFileInput(tempVideoPath)
					.OutputToPipe(new StreamPipeSink(outputStream), options => options
						.WithFrameOutputCount(1)
						.Seek(snapshotTime)
						.Resize(size)
						.ForceFormat("mjpeg")); // Важно указать формат для потокового вывода

				await arguments.ProcessAsynchronously();

				// После выполнения в outputStream уже содержатся данные изображения.
				// Временные файлы для скриншотов больше не нужны.
				if (outputStream.Length == 0)
				{
					// Если что-то пошло не так, поток будет пустым.
					throw new InvalidOperationException(
						$"FFMpeg не смог создать скриншот для момента времени {snapshotTime}. Проверьте логи FFmpeg.");
				}

				outputStream.Position = 0;
			}

			videoStream.Position = 0;
			return screenshots;
		}
		catch (Exception ex)
		{
			// Освобождаем память, если в процессе цикла произошла ошибка.
			foreach (var stream in screenshots)
			{
				await stream.DisposeAsync();
			}

			// Здесь можно добавить логирование ошибки
			throw new InvalidOperationException("Не удалось обработать видео с помощью FFMpeg.", ex);
		}
		finally
		{
			if (File.Exists(tempVideoPath))
			{
				File.Delete(tempVideoPath);
			}
		}
	}
}