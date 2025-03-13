using TgPoster.Domain.Services;

namespace TgPoster.Domain.UseCases.Messages.ListMessage;

public class FileResponse
{
    /// <summary>
    ///     Id файла
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    ///     Тип файла.
    /// </summary>
    public FileTypes FileType { get; set; }

    /// <summary>
    ///     Доступ до файла быстрый
    /// </summary>
    public Guid? FileCacheId { get; set; }

    /// <summary>
    ///     Доступ до превью видео быстрый
    /// </summary>
    public List<Guid> PreviewCacheIds { get; set; } = [];
}