using Microsoft.EntityFrameworkCore;
using TgPoster.Domain.UseCases.Messages.GetMessageById;
using TgPoster.Domain.UseCases.Messages.ListMessage;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages;

public class GetMessageStorage(PosterContext context) : IGetMessageStorage
{
    public async Task<MessageDto?> GetMessage(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var message = await context.Messages
            .Include(x => x.MessageFiles)
            .Include(x => x.Schedule)
            .Where(sch => sch.Schedule.UserId == userId)
            .Where(mess => mess.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
        if (message == null)
        {
            return null;
        }

        var response = new MessageDto
        {
            Id = message.Id,
            TextMessage = message.TextMessage,
            ScheduleId = message.ScheduleId,
            TimePosting = message.TimePosting,
            Files = message.MessageFiles.Select(f => new FileDto
            {
                Id = f.Id,
                ContentType = f.ContentType,
                TgFileId = f.TgFileId,
                PreviewIds = f is VideoMessageFile videoFile
                    ? videoFile.ThumbnailIds.ToList()
                    : []
            }).ToList()
        };

        return response;
    }
}