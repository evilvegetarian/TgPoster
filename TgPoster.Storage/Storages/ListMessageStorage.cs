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

    public async Task<PagedList<MessageDto>> GetMessagesAsync(
        Guid scheduleId,
        int pageNumber,
        int pageSize,
        string? requestSearchText,
        CancellationToken ct
    )
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

    public async Task<PagedList<MessageDto>> GetMessagesAsync(ListMessageQuery request, CancellationToken ct)
    {
        var query = context.Messages
            .Where(message => message.ScheduleId == request.ScheduleId);

        switch (request.Status)
        {
            case MessageStatus.All:
                break;
            case MessageStatus.Planed:
                query = query.Where(message => message.IsVerified);
                break;
            case MessageStatus.NotApproved:
                query = query.Where(message => !message.IsVerified);
                break;
            case MessageStatus.Delivered:
                query = query.Where(message => message.Status == Data.Enum.MessageStatus.Send);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        switch (request.SortBy)
        {
            case MessageSortBy.CreatedAt:
                query = request.SortDirection == SortDirection.Asc
                    ? query.OrderBy(m => m.Created)
                    : query.OrderByDescending(m => m.Created);
                break;
            case MessageSortBy.SentAt:
                query = request.SortDirection == SortDirection.Asc
                    ? query.OrderBy(m => m.TimePosting)
                    : query.OrderByDescending(m => m.TimePosting);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }


        if (!string.IsNullOrEmpty(request.SearchText))
        {
            query = query.Where(message =>
                message.TextMessage != null
                && message.TextMessage.Contains(request.SearchText, StringComparison.CurrentCultureIgnoreCase));
        }

        var totalCount = await query.CountAsync(ct);

        var messages = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
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