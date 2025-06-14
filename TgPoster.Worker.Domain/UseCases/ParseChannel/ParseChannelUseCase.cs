using Microsoft.Extensions.Logging;
using Security.Interfaces;
using Shared;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgPoster.Worker.Domain.ConfigModels;
using TL;
using WTelegram;
using Document = TL.Document;
using InputMediaPhoto = Telegram.Bot.Types.InputMediaPhoto;
using Message = TL.Message;

namespace TgPoster.Worker.Domain.UseCases.ParseChannel;

internal class ParseChannelUseCase(
    VideoService videoService,
    IParseChannelUseCaseStorage storage,
    TelegramSettings settings,
    TelegramOptions telegramOptions,
    ICryptoAES cryptoAes,
    TimePostingService timePostingService,
    ILogger<ParseChannelUseCase> logger)
{
    public async Task Handle(Guid id, CancellationToken ct)
    {
        var parametrs = await storage.GetChannelParsingParametersAsync(id, ct);
        if (parametrs is null)
        {
            logger.LogError("Параметров нет, интересно почему..... Id: {id}", id);
            return;
        }

        logger.LogInformation("Начали парсить данный канал с данными настройками: {@parametrs}", parametrs);
        await storage.UpdateInHandleStatusAsync(id, ct);

        var channelName = parametrs.ChannelName;
        var isNeedVerified = parametrs.IsNeedVerified;
        var token = cryptoAes.Decrypt(telegramOptions.SecretKey, parametrs.Token);
        var chatId = parametrs.ChatId;
        var fromDate = parametrs.FromDate;
        var toDate = parametrs.ToDate;
        var avoidWords = parametrs.AvoidWords;
        var deleteText = parametrs.DeleteText;
        var deleteMedia = parametrs.DeleteMedia;
        var scheduleId = parametrs.ScheduleId;
        var lastParseId = parametrs.LastParsedId;
        var checkNewPosts = parametrs.CheckNewPosts;
        //TODO: Добавить параметр перемешивания постов

        var telegramBot = new TelegramBotClient(token);
        await using var client = new Client(Settings);
        await client.LoginUserIfNeeded();

        var resolveResult = await client.Contacts_ResolveUsername(channelName);
        var channel = resolveResult.Chat as Channel;
        List<Message> allMessages = [];
        var tempLastParseId = 0;
        const int limit = 100;
        var offset = 0;
        while (true)
        {
            var history = await client.Messages_GetHistory(
                new InputPeerChannel(channel!.ID, channel.access_hash),
                limit: limit,
                offset_date: toDate ?? DateTime.Now,
                offset_id: offset
            );

            var messageFiltered = history.Messages
                .Where(x => fromDate is null || x.Date >= fromDate)
                .OfType<Message>()
                .Where(x => lastParseId is null || x.ID > lastParseId)
                .ToList();

            allMessages.AddRange(messageFiltered);

            if (history.Messages.Length is not 0)
            {
                var maxId = history.Messages.Max(x => x.ID);
                if (tempLastParseId < maxId)
                {
                    tempLastParseId = maxId;
                }
            }

            if (history.Messages.Length is 0)
            {
                break;
            }

            offset = history.Messages.Last().ID;

            if (history.Messages.Any(x => x.ID < lastParseId))
            {
                break;
            }

            if (history.Messages.Any(x => x.Date < fromDate))
            {
                break;
            }
        }

        var groupedMessages = new Dictionary<long, List<Message>>();

        long i = 1;
        foreach (var message in allMessages)
        {
            if (message.grouped_id != 0)
            {
                if (!groupedMessages.ContainsKey(message.grouped_id))
                {
                    groupedMessages[message.grouped_id] = [];
                }

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

        var avoids = groupedMessages
            .Where(group => !group.Value.Any(
                msg => msg.message != null
                       && avoidWords.Any(word => msg.message.Contains(word, StringComparison.OrdinalIgnoreCase))
            )).ToList();

        var result = new List<MessageDto>();
        foreach (var al in avoids)
        {
            var messagedto = new MessageDto
            {
                IsNeedVerified = isNeedVerified,
                ScheduleId = scheduleId
            };

            foreach (var message in al.Value)
            {
                await Task.Delay(1000, ct);
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
                            cancellationToken: ct);
                        var photoId = photoMessage.Photo?
                            .OrderByDescending(x => x.FileSize)
                            .Select(x => x.FileId)
                            .FirstOrDefault()!;
                        await telegramBot.DeleteMessage(chatId, photoMessage.MessageId, ct);
                        messagedto.Media.Add(new MediaDto
                        {
                            FileId = photoId,
                            MimeType = fileType
                        });
                    }
                    else if (message.media is MessageMediaDocument { document: Document doc })
                    {
                        var fileType = doc.mime_type.Split('/')[0];

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
                            var previews = await videoService.ExtractScreenshotsAsync(stream, 3);
                            album.AddRange(
                                previews.Select<MemoryStream, InputMediaPhoto>(preview =>
                                    new InputMediaPhoto(preview)));

                            var messages = await telegramBot.SendMediaGroup(
                                chatId,
                                album,
                                disableNotification: true,
                                cancellationToken: ct);
                            var previewPhotoIds = messages
                                .Select(m => m.Photo?
                                    .OrderByDescending(x => x.FileSize)
                                    .Select(x => x.FileId)
                                    .FirstOrDefault())
                                .Where(x => x != null)
                                .Distinct()
                                .ToList();
                            var fileVideoId = messages
                                .Select(m => m.Video?.FileId).FirstOrDefault();
                            foreach (var mess in messages)
                            {
                                await telegramBot.DeleteMessage(chatId, mess.MessageId, ct);
                            }

                            messagedto.Media.Add(new MediaDto
                            {
                                MimeType = doc.mime_type,
                                FileId = fileVideoId!,
                                PreviewPhotoIds = previewPhotoIds!
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

        if (!isNeedVerified)
        {
            var existTime = await storage.GetExistMessageTimePostingAsync(scheduleId, ct);
            var scheduleTime = await storage.GetScheduleTimeAsync(scheduleId, ct);
            var postingTime = timePostingService.GetTimeForPosting(result.Count, scheduleTime, existTime);
            if (postingTime.Count != 0)
            {
                for (var t = 0; t < Math.Min(result.Count, postingTime.Count); t++)
                    result[t].TimePosting = postingTime[t];
            }
        }

        await storage.CreateMessagesAsync(result, ct);
        await storage.UpdateChannelParsingParametersAsync(id, tempLastParseId, checkNewPosts, ct);
    }

    private string? Settings(string key)
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