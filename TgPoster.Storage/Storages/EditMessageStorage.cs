using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.Models;
using TgPoster.API.Domain.Services;
using TgPoster.API.Domain.UseCases.Messages.CreateMessagesFromFiles;
using TgPoster.API.Domain.UseCases.Messages.EditMessage;
using TgPoster.Storage.Data;
using TgPoster.Storage.Exception;
using TgPoster.Storage.Mapper;

namespace TgPoster.Storage.Storages;

internal sealed class EditMessageStorage(PosterContext context) : IEditMessageStorage
{
    public Task<bool> ExistMessageAsync(Guid messageId, Guid userId, CancellationToken ct)
    {
        return context.Messages
            .Where(m => m.Id == messageId)
            .Where(x => x.Schedule.UserId == userId)
            .AnyAsync(ct);
    }

    public async Task<List<FileDto>> GetFilesAsync(List<Guid> fileIds, CancellationToken ct)
    {
        return await context.MessageFiles
            .Where(f => fileIds.Contains(f.Id))
            .Select(f => new FileDto { Id = f.Id })
            .ToListAsync(ct);
    }

    public async Task UpdateMessageAsync(
        EditMessageCommand messageDto,
        List<MediaFileResult> newMediaFiles,
        CancellationToken ct
    )
    {
        var message = await context.Messages
            .Include(m => m.MessageFiles)
            .FirstOrDefaultAsync(m => m.Id == messageDto.Id, ct);

        message!.TextMessage = messageDto.Text;
        message.TimePosting = messageDto.TimePosting;
        message.ScheduleId = messageDto.ScheduleId;

        var files = message.MessageFiles.Where(f => messageDto.Files.Contains(f.Id)).ToList();
        message.MessageFiles = files;
        var newmediafiles = newMediaFiles.Select(file => file.ToEntity(messageDto.Id));
        foreach (var file in newmediafiles)
        {
            message.MessageFiles.Add(file);
        }

        await context.SaveChangesAsync(ct);
    }

    public Task<TelegramBotDto?> GetTelegramBotAsync(Guid messageId, Guid userId, CancellationToken ct)
    {
        return context.Messages
            .Where(x => x.Schedule.UserId == userId)
            .Where(x => x.Id == messageId)
            .Select(x => new TelegramBotDto
            {
                ApiTelegram = x.Schedule.TelegramBot.ApiTelegram,
                ChatId = x.Schedule.TelegramBot.ChatId
            }).FirstOrDefaultAsync(ct);
    }
}