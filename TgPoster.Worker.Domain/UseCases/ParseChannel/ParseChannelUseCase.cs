using OpenCvSharp;
using Security.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgPoster.Worker.Domain.ConfigModels;
using TL;
using WTelegram;
using Document = TL.Document;
using InputMediaPhoto = Telegram.Bot.Types.InputMediaPhoto;
using Message = TL.Message;

namespace TgPoster.Worker.Domain.UseCases.ParseChannel;

public class Parameters
{
    public string ChannelName { get; set; }
    public bool IsNeedVerified { get; set; }
    public string Token { get; set; }
    public long ChatId { get; set; }
    public bool DeleteText { get; set; }
    public bool DeleteMedia { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string[] AvoidWords { get; set; } = [];
    public int? LastParsedId { get; set; }
    public Guid ScheduleId { get; set; }
}

internal class ParseChannelUseCase(
    VideoService videoService,
    IParseChannelUseCaseStorage storage,
    TelegramSettings settings,
    TelegramOptions telegramOptions,
    ICryptoAES cryptoAES)
{
    public async Task Handle(Guid id, CancellationToken cancellationToken = default)
    {
        var parametrs = await storage.GetChannelParsingParameters(id, cancellationToken);
        if (parametrs is null)
        {
            throw new Exception();
        }
        var channelName = parametrs.ChannelName;
        var isNeedVerified = parametrs.IsNeedVerified;
        var token = cryptoAES.Decrypt(telegramOptions.SecretKey, parametrs.Token);
        var chatId = parametrs.ChatId;
        var fromDate = parametrs.FromDate;
        var toDate = parametrs.ToDate;
        var avoidWords = parametrs.AvoidWords;
        var deleteText = parametrs.DeleteText;
        var deleteMedia = parametrs.DeleteMedia;
        var scheduleId = parametrs.ScheduleId;
        var lastParseId = parametrs.LastParsedId ?? 0;

        var telegramBot = new TelegramBotClient(token);
        await using var client = new Client(Settings);
        await client.LoginUserIfNeeded();

        var resolveResult = await client.Contacts_ResolveUsername(channelName);
        var channel = resolveResult.Chat as Channel;
        List<Message> allMessages = [];

        const int limit = 100;
        while (true)
        {
            var history = await client.Messages_GetHistory(
                new InputPeerChannel(channel.ID, channel.access_hash),
                limit: limit,
                offset_date: toDate ?? DateTime.Now
            );
 
            var messageFiltred = history.Messages
                .Where(x => x.Date >= fromDate)
                .OfType<Message>()
                .Where(x => !avoidWords
                    .Any(w => x.message
                        .Contains(w, StringComparison.OrdinalIgnoreCase)))
                .Where(x => x.ID > lastParseId)
                .ToList();

            allMessages.AddRange(messageFiltred);

            if (history.Messages.Length is 0)
                break;
            if (history.Messages.Any(x => x.ID < lastParseId))
                break;
            if (history.Messages.Any(x => x.Date < fromDate))
                break;

            lastParseId = history.Messages.Last().ID;
        }

        var groupedMessages = new Dictionary<long, List<Message>>();

        long i = 1;
        foreach (var message in allMessages)
        {
            if (message.grouped_id != 0)
            {
                if (!groupedMessages.ContainsKey(message.grouped_id))
                    groupedMessages[message.grouped_id] = [];

                groupedMessages[message.grouped_id].Add(message);
            }
            else
            {
                groupedMessages[i++] =
                [
                    message
                ];
            }
        }

        var result = new List<MessageDto>();
        foreach (var al in groupedMessages)
        {
            var messagedto = new MessageDto
            {
                IsNeedVerified = isNeedVerified,
                ScheduleId = scheduleId
            };
            foreach (var message in al.Value)
            {
                if (message.media is not null && !deleteMedia)
                {
                    if (message.media is MessageMediaPhoto { photo: Photo photo })
                    {
                        var fileType = "image/jpeg";
                        using var stream = new MemoryStream();
                        await client.DownloadFileAsync(photo, stream);
                        stream.Position = 0;
                        var photoMessage = await telegramBot.SendPhoto(chatId,
                            new InputFileStream(stream),
                            cancellationToken: cancellationToken);
                        var photoId = photoMessage.Photo?
                            .OrderByDescending(x => x.FileSize)
                            .Select(x => x.FileId)
                            .FirstOrDefault();
                        await telegramBot.DeleteMessage(chatId, photoMessage.MessageId, cancellationToken);
                        messagedto.Media.Add(new MediaDto
                        {
                            FileId = photoId,
                            MimeType = fileType,
                        });
                    }
                    else if (message.media is MessageMediaDocument { document: Document doc })
                    {
                        var fileType = doc.mime_type.Split('/')[0];
                        Console.WriteLine($"Документ: {doc.id}, Тип: {doc.mime_type}");

                        if (fileType == "video")
                        {
                            using var stream = new MemoryStream();
                            await client.DownloadFileAsync(doc, stream);
                            stream.Position = 0;
                            var inputFile = new InputFileStream(stream, "file.FileName");
                            List<IAlbumInputMedia> album =
                            [
                                new InputMediaVideo { Media = inputFile }
                            ];
                            var previews = videoService.ExtractScreenshots(stream, 3);
                            album.AddRange(
                                previews.Select<MemoryStream, InputMediaPhoto>(preview =>
                                    new InputMediaPhoto(preview)));

                            var messages = await telegramBot.SendMediaGroup(
                                chatId,
                                album,
                                disableNotification: true,
                                cancellationToken: cancellationToken);
                            var previewPhotoIds = messages
                                .Select(m => m.Photo?
                                    .OrderByDescending(x => x.FileSize)
                                    .Select(x => x.FileId)
                                    .FirstOrDefault())
                                .Where(x => x != null)
                                .Distinct();
                            var fileVideoId = messages
                                .Select(m => m.Video?.FileId).FirstOrDefault();
                            foreach (var mess in messages)
                            {
                                await telegramBot.DeleteMessage(chatId, mess.MessageId, cancellationToken);
                            }

                            messagedto.Media.Add(new MediaDto
                            {
                                MimeType = doc.mime_type,
                                FileId = fileVideoId!,
                                PreviewPhotoIds = previewPhotoIds.ToList()
                            });
                        }
                    }
                }
                else if (!deleteText)
                {
                    messagedto.Text = message.message;
                }
            }

            if (messagedto.Media.Count > 0 || messagedto.Text is not null)
            {
                result.Add(messagedto);
            }
        }

        await storage.CreateMessages(result, cancellationToken);
        await storage.UpdateChannelParsingParameters(id, lastParseId, cancellationToken);
    }

    string? Settings(string key)
    {
        return key switch
        {
            nameof(settings.api_id) => settings.api_id,
            nameof(settings.api_hash) => settings.api_hash,
            nameof(settings.phone_number) => settings.phone_number,
            _ => null
        };
    }
}

public interface IParseChannelUseCaseStorage
{
    Task<Parameters?> GetChannelParsingParameters(Guid id, CancellationToken cancellationToken);
    Task CreateMessages(List<MessageDto> messages, CancellationToken cancellationToken);
    Task UpdateChannelParsingParameters(Guid id, int offsetId, CancellationToken cancellationToken);
}

public class MessageDto
{
    public string Text { get; set; }
    public Guid ScheduleId { get; set; }
    public bool IsNeedVerified { get; set; }
    public List<MediaDto> Media { get; set; } = [];
}

public class MediaDto
{
    public required string MimeType { get; set; }
    public required string FileId { get; set; }
    public List<string> PreviewPhotoIds { get; set; } = [];
}

internal sealed class VideoService
{
    public List<MemoryStream> ExtractScreenshots(MemoryStream videoStream, int screenshotCount, int outputWidth = 0)
    {
        if (videoStream == null)
            throw new ArgumentNullException(nameof(videoStream));
        if (screenshotCount < 1)
            throw new ArgumentException("Количество скриншотов должно быть не меньше 1", nameof(screenshotCount));

        var tempVideoPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".mp4");
        File.WriteAllBytes(tempVideoPath, videoStream.ToArray());

        var screenshots = new List<MemoryStream>();

        VideoCapture capture = null;
        try
        {
            capture = new VideoCapture(tempVideoPath);
            if (!capture.IsOpened())
                throw new ArgumentException("Не удалось открыть видео файл");

            // Получаем ключевые параметры видео.
            var fps = capture.Fps;
            var frameCount = capture.FrameCount;
            if (frameCount <= 0)
                throw new ArgumentException("Не удалось определить количество кадров");

            // Определяем длительность видео в секундах.
            var duration = frameCount / fps;

            // Для равномерного выбора кадров (без крайних), делим видео на screenshotCount+1 частей.
            // Вычисляем номера кадров для извлечения: для каждого скриншота определяем время, переводим в номер кадра.
            for (var i = 1; i <= screenshotCount; i++)
            {
                var snapshotTime = duration * i / (screenshotCount + 1); // в секундах
                var targetFrame = (int)(snapshotTime * fps);

                capture.Set(VideoCaptureProperties.PosFrames, targetFrame);

                using var frame = new Mat();
                if (!capture.Read(frame) || frame.Empty())
                    throw new ArgumentException($"Не удалось считать кадр под номером {targetFrame}");

                if (outputWidth > 0)
                {
                    var newWidth = outputWidth;
                    var newHeight = (int)(frame.Height * (outputWidth / (double)frame.Width));
                    Cv2.Resize(frame, frame, new Size(newWidth, newHeight));
                }

                Cv2.ImEncode(".jpg", frame, out var imageBytes);

                var screenshotStream = new MemoryStream(imageBytes);
                screenshots.Add(screenshotStream);
            }

            return screenshots;
        }
        finally
        {
            capture?.Release();
            if (File.Exists(tempVideoPath))
                File.Delete(tempVideoPath);
        }
    }
}