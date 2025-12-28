using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Caching.Memory;
using Shared;
using Telegram.Bot;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;

namespace TgPoster.API.Domain.Services;

internal sealed class FileService(
	IMemoryCache memoryCache,
	FileExtensionContentTypeProvider contentTypeProvider,
	IAmazonS3 s3,
	S3Options s3Options)
{
	/// <summary>
	///     Обрабатывает список файлов и возвращает список объектов с информацией о кешированном контенте.
	///     Для ContentTypes.Photo загружается и кэшируется изображение,
	///     для ContentTypes.Video кешируются только preview-изображения, основной видеофайл не кэшируется.
	/// </summary>
	/// <param name="botClient">Экземпляр TelegramBotClient для работы со скачиванием файлов.</param>
	/// <param name="fileDtos">Список DTO файлов для обработки.</param>
	/// <param name="ct">Токен отмены операции.</param>
	/// <returns>Список объектов FilesCacheInfo с информацией по кешу.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Если встречен неизвестный тип контента.</exception>
	public async Task<List<FilesCacheInfo>> ProcessFilesAsync(
		TelegramBotClient botClient,
		List<FileDto> fileDtos,
		CancellationToken ct
	)
	{
		var filesCacheInfoList = new List<FilesCacheInfo>();

		foreach (var fileDto in fileDtos)
		{
			var cacheInfo = new FilesCacheInfo
			{
				Id = fileDto.Id,
				FileType = fileDto.ContentType.GetFileType()
			};

			var fileType = fileDto.ContentType.GetFileType();
			switch (fileType)
			{
				case FileTypes.Image:
				{
					var cacheIdentifier = await DownloadAndCacheFileAsync(
						botClient,
						fileDto.TgFileId,
						ct);
					cacheInfo.FileCacheId = cacheIdentifier;
					break;
				}

				case FileTypes.Video:
				{
					foreach (var preview in fileDto.Previews)
					{
						var previewCacheId = await DownloadAndCacheFileAsync(
							botClient,
							preview.TgFileId,
							ct);
						cacheInfo.PreviewCacheIds.Add(previewCacheId);
					}

					break;
				}

				case FileTypes.NoOne:
				default:
					throw new ArgumentOutOfRangeException(nameof(fileType),
						$"Неизвестный тип контента: {fileType}");
			}

			filesCacheInfoList.Add(cacheInfo);
		}

		return filesCacheInfoList;
	}

	/// <summary>
	///     Скачивает файл и добавляет его в кеш.
	/// </summary>
	/// <param name="botClient">Экземпляр TelegramBotClient.</param>
	/// <param name="telegramFileId">Идентификатор файла в Telegram.</param>
	/// <param name="ct">Токен отмены операции.</param>
	/// <returns>Идентификатор файла, сохранённого в кеше.</returns>
	private async Task<Guid> DownloadAndCacheFileAsync(
		TelegramBotClient botClient,
		string telegramFileId,
		CancellationToken ct
	)
	{
		using var memoryStream = new MemoryStream();
		var file = await botClient.GetInfoAndDownloadFile(telegramFileId, memoryStream, ct);
		contentTypeProvider.TryGetContentType(file.FilePath, out var contentType);
		return CacheFile(memoryStream.ToArray(), contentType);
	}

	/// <summary>
	///     Скачивает файл из Telegram и кэширует его в S3, если его там еще нет.
	/// </summary>
	/// <param name="botClient">Экземпляр TelegramBotClient.</param>
	/// <param name="fileDtoId">Уникальный идентификатор файла, используемый как ключ в S3.</param>
	/// <param name="telegramFileId">Идентификатор файла в Telegram.</param>
	/// <param name="image"></param>
	/// <param name="ct">Токен отмены операции.</param>
	/// <returns>
	///     Возвращает true, если файл был успешно скачан и загружен в S3.
	///     Возвращает false, если файл уже существовал в S3.
	/// </returns>
	private async Task<bool> DownloadAndCacheS3FileAsync(
		TelegramBotClient botClient,
		Guid fileDtoId,
		string telegramFileId,
		FileTypes fileType,
		CancellationToken ct
	)
	{
		try
		{
			var getObjectRequest = new GetObjectMetadataRequest
			{
				BucketName = s3Options.BucketName,
				Key = fileDtoId.ToString()
			};
			await s3.GetObjectMetadataAsync(getObjectRequest, ct);
			return false;
		}
		catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
		{
		}

		await using var memoryStream = new MemoryStream();
		var file = await botClient.GetInfoAndDownloadFile(telegramFileId, memoryStream, ct);
		memoryStream.Position = 0;
		if (file.FilePath == null || !contentTypeProvider.TryGetContentType(file.FilePath, out var contentType))
		{
			contentType = fileType.GetContentType();
		}

		var request = new PutObjectRequest
		{
			BucketName = s3Options.BucketName,
			Key = fileDtoId.ToString(),
			ContentType = contentType,
			InputStream = memoryStream
		};

		var response = await s3.PutObjectAsync(request, ct);

		if (response.HttpStatusCode != HttpStatusCode.OK)
		{
			throw new InvalidOperationException(
				$"Failed to upload file {fileDtoId} to S3. Status: {response.HttpStatusCode}");
		}

		return true;
	}

	/// <summary>
	///     Сохраняет файл в cache и возвращает уникальный идентификатор.
	/// </summary>
	/// <param name="fileData">Байт-представление файла.</param>
	/// <param name="mimeType">MIME-тип файла.</param>
	/// <returns>GUID, представляющий файл в кеше.</returns>
	public Guid CacheFile(byte[] fileData, string mimeType)
	{
		var fileCacheItem = new FileCacheItem
		{
			Data = fileData,
			ContentType = mimeType
		};

		var fileCacheId = Guid.NewGuid();

		var memoryCacheOptions = new MemoryCacheEntryOptions
		{
			AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
		};
		memoryCache.Set(fileCacheId, fileCacheItem, memoryCacheOptions);

		return fileCacheId;
	}

	/// <summary>
	///     Извлекает файл из кеша по его идентификатору.
	/// </summary>
	/// <param name="cacheId">GUID файла в кеше.</param>
	/// <returns>Объект FileCacheItem, если файл найден; иначе null.</returns>
	public FileCacheItem? RetrieveFileFromCache(Guid cacheId) =>
		memoryCache.TryGetValue<FileCacheItem>(cacheId, out var fileCacheItem)
			? fileCacheItem
			: null;

	public async Task CacheFileToS3(TelegramBotClient botClient, List<FileDto> files, CancellationToken ct)
	{
		foreach (var fileDto in files)
		{
			var fileType = fileDto.ContentType.GetFileType();
			switch (fileType)
			{
				case FileTypes.Image:
				{
					await DownloadAndCacheS3FileAsync(botClient, fileDto.Id, fileDto.TgFileId, FileTypes.Image, ct);
					break;
				}

				case FileTypes.Video:
				{
					foreach (var preview in fileDto.Previews)
					{
						await DownloadAndCacheS3FileAsync(botClient, preview.Id, preview.TgFileId, FileTypes.Video, ct);
					}

					break;
				}
				case FileTypes.NoOne:
				default:
					throw new ArgumentOutOfRangeException(nameof(fileType), $"Неизвестный тип контента: {fileType}");
			}
		}
	}
}

public class FilesCacheInfo
{
	public required Guid Id { get; set; }
	public required FileTypes FileType { get; set; }
	public Guid? FileCacheId { get; set; }
	public List<Guid> PreviewCacheIds { get; set; } = [];
}

public class FileCacheItem
{
	public required byte[] Data { get; set; }
	public required string ContentType { get; set; }
}