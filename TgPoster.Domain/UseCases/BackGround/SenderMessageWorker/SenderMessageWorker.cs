using Hangfire;
using Microsoft.Extensions.Logging;
using Security.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgPoster.Domain.ConfigModels;
using TgPoster.Domain.Services;

namespace TgPoster.Domain.UseCases.BackGround.SenderMessageWorker;

public class SenderMessageWorker(
    ISenderMessageStorage storage,
    ILogger<SenderMessageWorker> logger,
    ICryptoAES crypto,
    TelegramOptions options
)
{
    public async Task ProcessMessagesAsync()
    {
        logger.LogInformation("Начат процесс проверки новых сообщений.");

        var messageDetails = await storage.GetMessagesAsync();

        foreach (var detail in messageDetails)
        {
            var token = crypto.Decrypt(options.SecretKey, detail.Api);

            foreach (var message in detail.MessageDto.OrderBy(x => x.TimePosting))
            {
                BackgroundJob.Schedule<SenderMessageWorker>(
                    x => x.SendMessageAsync(message.Id, token, detail.ChannelId, message),
                    message.TimePosting);
                logger.LogInformation(
                    $"Сообщение для чата {detail.ChannelId} запланировано на {message.TimePosting} сек.");
            }
        }
    }

    public async Task SendMessageAsync(Guid messageId, string token, long chatId, MessageDto message)
    {
        var bot = new TelegramBotClient(token);
        var medias = new List<IAlbumInputMedia>();
        foreach (var file in message.File)
        {
            if (file.ContentType.GetFileType() == FileTypes.Image)
            {
                medias.Add(new InputMediaPhoto(file.TgFileId)
                {
                    Caption = file.Caption,
                });
            }

            if (file.ContentType.GetFileType() == FileTypes.Video)
            {
                medias.Add(new InputMediaVideo(file.TgFileId)
                {
                    Caption = file.Caption,
                });
            }
        }

        if (medias.Any())
        {
            await bot.SendMediaGroup(chatId, medias);
        }
        else
        {
            await bot.SendMessage(chatId, message.Message!);
        }

        await storage.UpdateStatusMessage(messageId);
        logger.LogInformation($"Отправлено сообщение в чат {chatId}");
    }
}