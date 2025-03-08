using TgPoster.Domain.Services;

namespace TgPoster.Domain.UseCases.Messages.ListMessage;

public class FileResponse
{
    public ContentTypes ContentType { get; set; }
    public Guid? FileCacheId { get; set; }
    public List<Guid> PreviewCacheIds { get; set; } = [];
}