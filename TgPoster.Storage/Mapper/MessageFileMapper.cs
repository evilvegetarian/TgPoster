using TgPoster.API.Domain.Services;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Mapper;

internal static class MessageFileMapper
{
	public static List<MessageFile> ToEntity(this MediaFileResult file, Guid messageId, int order)
	{
		var guidFactory = new GuidFactory();
		var messageFileId = guidFactory.New();
		var files = new List<MessageFile>();

		var mainFile = new MessageFile
		{
			Id = messageFileId,
			MessageId = messageId,
			TgFileId = file.FileId,
			ContentType = file.MimeType,
			FileType = (FileTypes)file.FileType,
			ParentFileId = null,
			Order = order
		};
		files.Add(mainFile);

		var thumbnails = file.PreviewPhotoIds.Select(x => new MessageFile
		{
			Id = guidFactory.New(),
			MessageId = messageId,
			TgFileId = x,
			ContentType = "image/jpeg",
			FileType = FileTypes.Thumbnail,
			ParentFileId = messageFileId,
			Order = 0
		}).ToList();

		files.AddRange(thumbnails);
		return files;
	}
}