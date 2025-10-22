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
using MessageEntity = Telegram.Bot.Types.MessageEntity;

namespace TgPoster.Worker.Domain.UseCases.ParseChannel;

internal class ParseChannelUseCase(
    VideoService videoService,
    IParseChannelUseCaseStorage storage,
    TelegramSettings settings,
    TelegramOptions telegramOptions,
    ICryptoAES cryptoAes,
    TimePostingService timePostingService,
    TelegramExecuteServices telegramExecuteServices,
    ILogger<ParseChannelUseCase> logger)
{
    public async Task Handle(Guid id, CancellationToken ct)
    {
        var parameters = await storage.GetChannelParsingParametersAsync(id, ct);
        if (parameters is null)
        {
            logger.LogError("Параметров нет, интересно почему..... Id: {id}", id);
            return;
        }

        logger.LogInformation("Начали парсить данный канал: {@parameters}", parameters.ChannelName);
        await storage.UpdateInHandleStatusAsync(id, ct);

        var channelName = parameters.ChannelName;
        var isNeedVerified = parameters.IsNeedVerified;
        var token = cryptoAes.Decrypt(telegramOptions.SecretKey, parameters.Token);
        var chatId = parameters.ChatId;
        var fromDate = parameters.FromDate;
        var toDate = parameters.ToDate;
        var avoidWords = parameters.AvoidWords;
        var deleteText = parameters.DeleteText;
        var deleteMedia = parameters.DeleteMedia;
        var scheduleId = parameters.ScheduleId;
        var lastParseId = parameters.LastParsedId;
        var checkNewPosts = parameters.CheckNewPosts;
        //TODO: Добавить параметр перемешивания постов

        var telegramBot = new TelegramBotClient(token);
        await using var client = new Client(Settings);
        await client.LoginUserIfNeeded();

        var resolveResult = await client.Contacts_ResolveUsername(channelName);
        var channel = resolveResult.Chat as Channel;
        List<Message> allMessages = [];
        var tempLastParseId = 0;
        const int limit = 100;
        var offset = lastParseId ?? 0;
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

            try
            {
                foreach (var message in al.Value)
                {
                    await Task.Delay(5000, ct);
                    if (message.media is not null && !deleteMedia)
                    {
                        if (message.media is MessageMediaPhoto { photo: Photo photo })
                        {
                            var fileType = "image/jpeg";
                            using var stream = new MemoryStream();
                            await telegramExecuteServices.DownloadPhotoAsync(client, channel, photo, stream, message.ID);
                            stream.Position = 0;
                            var photoStream = new InputFileStream(stream);
                            var photoMessage = await telegramExecuteServices.SendPhoto(telegramBot,chatId, photoStream,3,ct);
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

                                await telegramExecuteServices.DownloadVideoAsync(client, channel, doc, stream, message.ID);

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

                                var messages = await telegramExecuteServices.SendMedia(telegramBot, chatId, album,3,ct);
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
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ошибка во время парсинга медиа. Разберись что ли в будущем");
            }

            if (messagedto.Media.Count > 0 || messagedto.Text is not null)
            {
                result.Add(messagedto);
            }
        }

        var existTime = await storage.GetExistMessageTimePostingAsync(scheduleId, ct);
        var scheduleTime = await storage.GetScheduleTimeAsync(scheduleId, ct);
        var postingTime = timePostingService.GetTimeForPosting(result.Count, scheduleTime, existTime);
        if (postingTime.Count != 0)
        {
            for (var t = 0; t < Math.Min(result.Count, postingTime.Count); t++)
                result[t].TimePosting = postingTime[t];
        }

        await storage.CreateMessagesAsync(result, ct);
        await storage.UpdateChannelParsingParametersAsync(id, tempLastParseId, checkNewPosts, ct);
        logger.LogInformation("Спарсили канал, новых сообщений: {count}", result.Count);
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

public class TelegramExecuteServices(ILogger<TelegramExecuteServices> logger)
{
    public async Task<Telegram.Bot.Types.Message[]> SendMedia(TelegramBotClient telegramBot, long chatId, List<IAlbumInputMedia> album, int maxRetries, CancellationToken ct)
    {
        var retryCount = 0;
        while (true)
        {
            try
            {
                var messages = await telegramBot.SendMediaGroup(
                    chatId,
                    album,
                    disableNotification: true,
                    cancellationToken: ct);
                return messages;
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.ErrorCode == 429)
            {
                retryCount++;
                if (retryCount > maxRetries)
                {
                    logger.LogError(ex, "Достигнуто максимальное количество попыток для API вызова. Отказ.");
                    throw;
                }

                var retryAfter = ex.Parameters?.RetryAfter ?? 30;
                var waitTime = TimeSpan.FromSeconds(retryAfter + 1);

                logger.LogWarning(
                    "Получен лимит запросов от Telegram API. Ожидание: {WaitTime} сек. Попытка {RetryCount}/{MaxRetries}",
                    retryAfter, retryCount, maxRetries);

                await Task.Delay(waitTime, ct);
            }
        }
    }

    public async Task<Telegram.Bot.Types.Message> SendPhoto(TelegramBotClient telegramBot, long chatId, InputFileStream photoStream, int maxRetries, CancellationToken ct)
    {
        var retryCount = 0;
        while (true)
        {
            try
            {
                var photoMessage = await telegramBot.SendPhoto(chatId, photoStream, cancellationToken: ct);
                return photoMessage;
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.ErrorCode == 429)
            {
                retryCount++;
                if (retryCount > maxRetries)
                {
                    logger.LogError(ex, "Достигнуто максимальное количество попыток для API вызова. Отказ.");
                    throw;
                }

                var retryAfter = ex.Parameters?.RetryAfter ?? 30;
                var waitTime = TimeSpan.FromSeconds(retryAfter + 1);

                logger.LogWarning(
                    "Получен лимит запросов от Telegram API. Ожидание: {WaitTime} сек. Попытка {RetryCount}/{MaxRetries}",
                    retryAfter, retryCount, maxRetries);

                await Task.Delay(waitTime, ct);
            }
        }
    }

public async Task DownloadVideoAsync(
        Client client,
        Channel channel,
        Document video,
        Stream stream,
        int messageId
    )
    {
        try
        {
            await client.DownloadFileAsync(video, stream);
        }
        catch (RpcException ex) when (ex.Message == "FILE_REFERENCE_EXPIRED")
        {
            var refreshedMessages = await client.Channels_GetMessages(new InputChannel(channel.id, channel.access_hash), messageId);

            if (refreshedMessages.Messages.FirstOrDefault() is Message
                {
                    media: MessageMediaDocument { document: Document refreshedDoc }
                })
            {
                await client.DownloadFileAsync(refreshedDoc, stream);
            }
            else
            {
                throw;
            }
        }
    }
    
    public async Task DownloadPhotoAsync(
        Client client,
        Channel channel,
        Photo photo,
        Stream stream,
        int messageId
    )
    {
        try
        {
            await client.DownloadFileAsync(photo, stream);
        }
        catch (RpcException ex) when (ex.Message == "FILE_REFERENCE_EXPIRED")
        {
            var refreshedMessages = await client.Channels_GetMessages(new InputChannel(channel.id, channel.access_hash), messageId);

            if (refreshedMessages.Messages.FirstOrDefault() is Message
                {
                    media: MessageMediaPhoto { photo: Photo refreshedPhoto }
                })
            {
                await client.DownloadFileAsync(refreshedPhoto, stream);
            }
            else
            {
                throw;
            }
        }
    }



    /// <summary>
    /// Выполняет асинхронную операцию с обработкой ошибок ограничения скорости Telegram.
    /// </summary>
    /// <param name="apiCall">Функция, представляющая вызов API Telegram, не возвращающая результат.</param>
    /// <param name="maxRetries">Максимальное количество повторных попыток.</param>
    /// <param name="ct">Токен отмены.</param>
    private async Task ExecuteWithRetryAsync(Func<Task> apiCall, int maxRetries = 3, CancellationToken ct = default)
    {
        var retryCount = 0;
        while (true)
        {
            try
            {
                await apiCall();
                return;
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.ErrorCode == 429)
            {
                retryCount++;
                if (retryCount > maxRetries)
                {
                    logger.LogError(ex, "Достигнуто максимальное количество попыток для API вызова. Отказ.");
                    throw;
                }

                var retryAfter = ex.Parameters?.RetryAfter ?? 30;
                var waitTime = TimeSpan.FromSeconds(retryAfter + 1);

                logger.LogWarning(
                    "Получен лимит запросов от Telegram API. Ожидание: {WaitTime} сек. Попытка {RetryCount}/{MaxRetries}",
                    retryAfter, retryCount, maxRetries);

                await Task.Delay(waitTime, ct);
            }
        }
    }

    /// <summary>
    /// Выполняет асинхронную операцию с обработкой ошибок ограничения скорости Telegram.
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения.</typeparam>
    /// <param name="apiCall">Функция, представляющая вызов API Telegram, возвращающая результат типа T.</param>
    /// <param name="maxRetries">Максимальное количество повторных попыток.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Результат выполнения API вызова.</returns>
    private async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> apiCall,
        int maxRetries = 3,
        CancellationToken ct = default
    )
    {
        var retryCount = 0;
        while (true)
        {
            try
            {
                return await apiCall();
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.ErrorCode == 429)
            {
                retryCount++;
                if (retryCount > maxRetries)
                {
                    logger.LogError(ex, "Достигнуто максимальное количество попыток для API вызова. Отказ.");
                    throw;
                }

                var retryAfter = ex.Parameters?.RetryAfter ?? 30;
                var waitTime = TimeSpan.FromSeconds(retryAfter + 1);

                logger.LogWarning(
                    "Получен лимит запросов от Telegram API. Ожидание: {WaitTime} сек. Попытка {RetryCount}/{MaxRetries}",
                    retryAfter, retryCount, maxRetries);

                await Task.Delay(waitTime, ct);
            }
        }
    }
}