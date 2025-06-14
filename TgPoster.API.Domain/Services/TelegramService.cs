using Microsoft.AspNetCore.Http;
using Shared;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TgPoster.API.Domain.Services;

internal sealed class TelegramService(VideoService videoService)
{
    public async Task<List<MediaFileResult>> GetFileMessageInTelegramByFile(
        TelegramBotClient botClient,
        List<IFormFile> files,
        long chatIdWithBotUser,
        CancellationToken ct
    )
    {
        var chat = await botClient.GetChat(chatIdWithBotUser, ct);

        List<MediaFileResult> media = [];
        foreach (var file in files)
        {
            await using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, ct);
            memoryStream.Position = 0;
            var inputFile = new InputFileStream(memoryStream, file.FileName);
            var type = file.GetFileType();
            switch (type)
            {
                case FileTypes.Video:
                    List<IAlbumInputMedia> album =
                    [
                        new InputMediaVideo { Media = inputFile }
                    ];
                    var previews =await videoService.ExtractScreenshotsAsync(memoryStream, 3);
                    album.AddRange(previews.Select(preview => new InputMediaPhoto(preview)));

                    var messages = await botClient.SendMediaGroup(
                        chat.Id,
                        album,
                        disableNotification: true,
                        cancellationToken: ct);
                    var previewPhotoIds = messages
                        .Where(m => m.Photo != null && m.Photo.Length != 0)
                        .Select(m => m.Photo!.OrderByDescending(x => x.FileSize)
                            .Select(x => x.FileId)
                            .FirstOrDefault())
                        .Where(x => x != null)
                        .Distinct()
                        .ToList();
                    var fileVideoId = messages
                        .Select(m => m.Video?.FileId).FirstOrDefault();
                    foreach (var mess in messages)
                    {
                        await botClient.DeleteMessage(chat.Id, mess.MessageId, ct);
                    }

                    media.Add(new MediaFileResult
                    {
                        MimeType = file.ContentType,
                        FileId = fileVideoId!,
                        PreviewPhotoIds = previewPhotoIds!
                    });
                    break;

                case FileTypes.Image:
                    var message = await botClient.SendPhoto(
                        chat.Id,
                        inputFile,
                        disableNotification: true,
                        cancellationToken: ct);
                    var photoId = message.Photo?
                        .OrderByDescending(x => x.FileSize)
                        .Select(x => x.FileId)
                        .FirstOrDefault();
                    media.Add(new MediaFileResult
                    {
                        MimeType = file.ContentType,
                        FileId = photoId!
                    });
                    await botClient.DeleteMessage(chat.Id, message.MessageId, ct);
                    break;

                case FileTypes.NoOne:
                    break;

                default:
                    throw new Exception("Тип данных какой то странный, изучи этот вопрос");
            }
        }

        return media;
    }
}

public class MediaFileResult
{
    public required string MimeType { get; set; }
    public required string FileId { get; set; }
    public List<string> PreviewPhotoIds { get; set; } = [];
}