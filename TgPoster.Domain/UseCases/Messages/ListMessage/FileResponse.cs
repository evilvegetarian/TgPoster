using TgPoster.Domain.Services;

namespace TgPoster.Domain.UseCases.Messages.ListMessage;

public class FileResponse
{
    public FileTypes FileType { get; set; }
    public Guid? FileCacheId { get; set; }
    public List<Guid> PreviewCacheIds { get; set; } = [];
}