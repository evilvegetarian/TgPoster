using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Caching.Memory;
using Telegram.Bot;
using TgPoster.Domain.UseCases.Messages.ListMessage;

namespace TgPoster.Domain.Services;

internal sealed class FileService(IMemoryCache memoryCache, FileExtensionContentTypeProvider contentTypeProvider)
{
    /// <summary>
    ///     Обрабатывает список файлов и возвращает список объектов с информацией о кешированном контенте.
    ///     Для ContentTypes.Photo загружается и кэшируется изображение,
    ///     для ContentTypes.Video кешируются только preview-изображения, основной видеофайл не кэшируется.
    /// </summary>
    /// <param name="botClient">Экземпляр TelegramBotClient для работы со скачиванием файлов.</param>
    /// <param name="fileDtos">Список DTO файлов для обработки.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Список объектов FilesCacheInfo с информацией по кешу.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Если встречен неизвестный тип контента.</exception>
    public async Task<List<FilesCacheInfo>> ProcessFilesAsync(
        TelegramBotClient botClient,
        List<FileDto> fileDtos,
        CancellationToken cancellationToken
    )
    {
        var filesCacheInfoList = new List<FilesCacheInfo>();

        foreach (var fileDto in fileDtos)
        {
            var cacheInfo = new FilesCacheInfo
            {
                FileType = fileDto.ContentType.GetFileType()
            };

            var fileType = fileDto.ContentType.GetFileType();
            switch (fileType)
            {
                case FileTypes.Photo:
                {
                    var cacheIdentifier = await DownloadAndCacheFileAsync(
                        botClient,
                        fileDto.TgFileId,
                        cancellationToken);
                    cacheInfo.FileCacheId = cacheIdentifier;
                    break;
                }

                case FileTypes.Video:
                {
                    foreach (var previewFileId in fileDto.PreviewIds)
                    {
                        var previewCacheId = await DownloadAndCacheFileAsync(
                            botClient,
                            previewFileId,
                            cancellationToken);
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
    /// <param name="mimeType">MIME-тип файла.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Идентификатор файла, сохранённого в кеше.</returns>
    private async Task<Guid> DownloadAndCacheFileAsync(
        TelegramBotClient botClient,
        string telegramFileId,
        CancellationToken cancellationToken
    )
    {
        using var memoryStream = new MemoryStream();
        var file = await botClient.GetInfoAndDownloadFile(telegramFileId, memoryStream, cancellationToken);
        contentTypeProvider.TryGetContentType(file.FilePath, out var contentType);
        return CacheFile(memoryStream.ToArray(), contentType);
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
}

public class FilesCacheInfo
{
    public FileTypes FileType { get; set; }
    public Guid? FileCacheId { get; set; }
    public List<Guid> PreviewCacheIds { get; set; } = [];
}

public class FileCacheItem
{
    public required byte[] Data { get; set; }
    public required string ContentType { get; set; }
}