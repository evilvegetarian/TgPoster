using TL;
using WTelegram;

namespace TgPoster.Worker.Domain.UseCases.ParseChannel;

public class ParseChannelUseCase(IParseChannelUseCaseStorage storage)
{
    public async Task Handle(Guid id, CancellationToken cancellationToken = default)
    {
        var parametrs = await storage.GetChannelParsingParameters(id, cancellationToken);
        await using var client = new Client(Settings);
        await client.LoginUserIfNeeded();

        var resolveResult = await client.Contacts_ResolveUsername(parametrs);
        var channel = resolveResult.Chat as Channel;
        List<Message> allMessages = [];
        int offsetId = 0;

        const int limit = 100;
        while (true)
        {
            var history = await client.Messages_GetHistory(
                new InputPeerChannel(channel.ID, channel.access_hash),
                limit: limit,
                offset_id: offsetId
            );

            if (history.Messages.Length is 0)
                break;
            if (allMessages.Count >= 1000)
                break;

            allMessages.AddRange(history.Messages.OfType<Message>());

            offsetId = history.Messages.Last().ID;
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

        foreach (var album in groupedMessages)
        {
            var messages = album.Value;
            foreach (var message in messages)
            {
                if (message.media is not null)
                {
                    if (message.media is MessageMediaPhoto photoMedia && photoMedia.photo is Photo photo)
                    {
                        using var stream = new MemoryStream();
                        var dsa = await client.DownloadFileAsync(photo, stream);
                        await File.WriteAllBytesAsync("C:\\Users\\veget\\Downloads\\photo2.jpg", stream.ToArray());
                        Console.WriteLine($"Фото ID: {photo.id}");
                    }
                    else if (message.media is MessageMediaDocument docMedia && docMedia.document is Document doc)
                    {
                        var fileType = doc.mime_type.Split('/')[0];
                        Console.WriteLine($"Документ: {doc.id}, Тип: {doc.mime_type}");

                        if (fileType == "video")
                        {
                            using var stream = new MemoryStream();
                            var dsa = await client.DownloadFileAsync(doc, stream);
                            Console.WriteLine("Это видео файл");
                        }
                    }
                }
                else
                {
                }
            }
        }

        static string? Settings(string key)
        {
            return key switch
            {
                _ => null
            };
        }
    }
}

public interface IParseChannelUseCaseStorage
{
    Task<string> GetChannelParsingParameters(Guid id, CancellationToken cancellationToken);
}