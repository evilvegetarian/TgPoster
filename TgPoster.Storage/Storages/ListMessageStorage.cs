using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.Models;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using FileDto = TgPoster.API.Domain.UseCases.Messages.ListMessage.FileDto;
using MessageDto = TgPoster.API.Domain.UseCases.Messages.ListMessage.MessageDto;

namespace TgPoster.Storage.Storages;

internal sealed class ListMessageStorage(PosterContext context) : IListMessageStorage
{
    public Task<bool> ExistScheduleAsync(Guid scheduleId, Guid userId, CancellationToken ct)
    {
        return context.Schedules.AnyAsync(x => x.Id == scheduleId && x.UserId == userId, ct);
    }

    public Task<string?> GetApiTokenAsync(Guid scheduleId, CancellationToken ct)
    {
        return context.Schedules
            .Where(x => x.Id == scheduleId)
            .Select(x => x.TelegramBot.ApiTelegram)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<PagedList<MessageDto>> GetMessagesAsync(Guid scheduleId, int pageNumber, int pageSize, CancellationToken ct)
    {
        var query = context.Messages
            .Where(message => message.ScheduleId == scheduleId)
            .OrderByDescending(m => m.TimePosting); 

        var totalCount = await query.CountAsync(ct);

        var messages = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(message => message.MessageFiles)
            .ToListAsync(ct);

        var messageDtos = messages.Select(message => new MessageDto
        {
            Id = message.Id,
            TextMessage = message.TextMessage,
            ScheduleId = message.ScheduleId,
            TimePosting = message.TimePosting,
            IsVerified = message.IsVerified,
            Files = message.MessageFiles.Select(file => new FileDto
            {
                Id = file.Id,
                ContentType = file.ContentType,
                TgFileId = file.TgFileId,
                PreviewIds = file is VideoMessageFile videoFile
                    ? videoFile.ThumbnailIds.ToList()
                    : []
            }).ToList()
        }).ToList();
        
        return new PagedList<MessageDto>(messageDtos, totalCount);
    }
}