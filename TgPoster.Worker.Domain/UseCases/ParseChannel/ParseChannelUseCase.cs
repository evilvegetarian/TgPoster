using Security.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgPoster.Worker.Domain.ConfigModels;
using TL;
using WTelegram;
using Shared;
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
        var parametrs = await storage.GetChannelParsingParametersAsync(id, cancellationToken);
        if (parametrs is null)
        {
            throw new Exception();
        }

        await storage.UpdateInHandleStatusAsync(id, cancellationToken);

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
        var lastParseId = parametrs.LastParsedId;

        var telegramBot = new TelegramBotClient(token);
        await using var client = new Client(Settings);
        await client.LoginUserIfNeeded();

        var resolveResult = await client.Contacts_ResolveUsername(channelName);
        var channel = resolveResult.Chat as Channel;
        List<Message> allMessages = [];
        var tempLastParseId = 0;
        const int limit = 100;
        int offset = 0;
        while (true)
        {
            var history = await client.Messages_GetHistory(
                new InputPeerChannel(channel.ID, channel.access_hash),
                limit: limit,
                offset_date: toDate ?? DateTime.Now,
                offset_id: offset
            );

            var messageFiltred = history.Messages
                .Where(x => fromDate is null || x.Date >= fromDate)
                .OfType<Message>()
                .Where(x => lastParseId is null || x.ID > lastParseId)
                .ToList();

            allMessages.AddRange(messageFiltred);
            if (tempLastParseId < history.Messages.Max(x => x.ID))
            {
                tempLastParseId = history.Messages.Max(x => x.ID);
            }

            offset = history.Messages.Last().ID;
            if (history.Messages.Length is 0)
                break;
            if (history.Messages.Any(x => x.ID < lastParseId))
                break;
            if (history.Messages.Any(x => x.Date < fromDate))
                break;
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
                await Task.Delay(1000, cancellationToken);
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

            if (messagedto.Media.Count > 0
                || (messagedto.Text is not null
                    && !avoidWords
                        .Any(w => messagedto.Text
                            .Contains(w, StringComparison.OrdinalIgnoreCase))))
            {
                result.Add(messagedto);
            }
        }

        await storage.CreateMessagesAsync(result, cancellationToken);
        await storage.UpdateChannelParsingParametersAsync(id, tempLastParseId, cancellationToken);
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
    Task<Parameters?> GetChannelParsingParametersAsync(Guid id, CancellationToken cancellationToken);
    Task CreateMessagesAsync(List<MessageDto> messages, CancellationToken cancellationToken);
    Task UpdateChannelParsingParametersAsync(Guid id, int offsetId, CancellationToken cancellationToken);
    Task UpdateInHandleStatusAsync(Guid id, CancellationToken cancellationToken);
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

