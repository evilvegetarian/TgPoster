using TgPoster.API.Domain.Services;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Mapper;

internal static class MessageFileMapper
{
	public static MessageFile ToEntity(this MediaFileResult file, Guid messageId)
	{
		var guidFactory = new GuidFactory();
		var messageFileId = guidFactory.New();
		return new MessageFile
		{
			Id = messageFileId,
			MessageId = messageId,
			TgFileId = file.FileId,
			ContentType = file.MimeType,
			FileType = (FileTypes)file.FileType,
			Thumbnails = file.PreviewPhotoIds.Select(x => new FileThumbnail
			{
				Id = guidFactory.New(),
				TgFileId = x,
				ContentType = "image/jpeg",
				MessageFileId = messageFileId
			}).ToList()
		};
	}
}