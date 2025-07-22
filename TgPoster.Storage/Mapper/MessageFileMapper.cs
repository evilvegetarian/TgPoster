using TgPoster.API.Domain.Services;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Mapper;

internal static class MessageFileMapper
{
    public static MessageFile ToEntity(this MediaFileResult file, Guid messageId)
    {
        var guidFactory = new GuidFactory();
        MessageFile messageFile = !file.PreviewPhotoIds.Any()
            ? new PhotoMessageFile
            {
                Id = guidFactory.New(),
                MessageId = messageId,
                TgFileId = file.FileId,
                ContentType = file.MimeType
            }
            : new VideoMessageFile
            {
                Id = guidFactory.New(),
                MessageId = messageId,
                TgFileId = file.FileId,
                ContentType = file.MimeType,
                ThumbnailIds = file.PreviewPhotoIds
            };
        return messageFile;
    }
}