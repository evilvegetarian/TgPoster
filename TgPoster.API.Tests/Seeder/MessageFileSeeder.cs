using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Endpoint.Tests.Seeder;

internal class MessageFileSeeder(PosterContext context) : BaseSeeder
{
    public override async Task Seed()
    {
        var files = new List<MessageFile>
        {
            new VideoMessageFile
            {
                Id = Guid.Parse("b0ab80b6-5b6f-4a69-88b2-2bffc5528ce3"),
                MessageId = GlobalConst.MessageId,
                ContentType = "image/jpeg",
                //TODO:возможно в будущем ошибки будут
                TgFileId = "randomTgFile"
            },
            new PhotoMessageFile
            {
                Id = Guid.Parse("49835027-a120-4ce1-bcd7-27d1bcd4e7aa"),
                MessageId = GlobalConst.MessageId,
                ContentType = "image/jpeg",
                //TODO:возможно в будущем ошибки будут
                TgFileId = "randomTgFile"
            },
            new PhotoMessageFile
            {
                Id = Guid.Parse("f54d2728-d33c-4e84-aead-ae7c7ee5422a"),
                MessageId = GlobalConst.MessageId,
                ContentType = "image/jpeg",
                //TODO:возможно в будущем ошибки будут
                TgFileId = "randomTgFile"
            }
        };

        await context.MessageFiles.AddRangeAsync(files);
    }
}