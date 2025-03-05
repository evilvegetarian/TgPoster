using System.Net;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Security.Interfaces;
using Telegram.Bot;
using TgPoster.Domain.ConfigModels;
using TgPoster.Domain.Services;
using TgPoster.Domain.UseCases.Files;

namespace TgPoster.Domain.UseCases.Messages.ListMessage;

public sealed record ListMessageQuery(Guid ScheduleId) : IRequest<List<MessageResponse>>;

internal sealed class ListMessageUseCase(
    IListMessageStorage storage,
    IOptions<TelegramOptions> options,
    ICryptoAES aes,
    FileService fileService
) : IRequestHandler<ListMessageQuery, List<MessageResponse>>
{
    public async Task<List<MessageResponse>> Handle(ListMessageQuery request, CancellationToken cancellationToken)
    {
        if (await storage.ExistSchedule(request.ScheduleId, cancellationToken))
            throw new Exception();

        var encryptedToken = await storage.GetApiToken(request.ScheduleId, cancellationToken);
        if (encryptedToken == null)
            throw new Exception();

        var token = aes.Encrypt(options, encryptedToken);

        var bot = new TelegramBotClient(token);
        var message = await storage.GetMessagesAsync(request.ScheduleId, cancellationToken);
        var messages = new List<MessageResponse>();
        foreach (var m in message)
        {
            var filesCacheInfos = await fileService.ProcessFilesAsync(bot, m.Files, cancellationToken);
            messages.Add(new MessageResponse
            {
                Id = m.Id,
                TextMessage = m.TextMessage,
            });
        }

        return messages;
    }
}

public class MessageResponse
{
    public required Guid Id { get; set; }
    public string? TextMessage { get; set; }
    public List<string> Files { get; set; } = [];
}

public sealed class MessageDto
{
    public required Guid Id { get; set; }
    public string? TextMessage { get; set; }
    public List<FileDto> Files { get; set; } = [];
}

public sealed class FileDto
{
    public required Guid Id { get; set; }
    public required string TgFileId { get; set; }
    public required ContentTypes Type { get; set; }
    public List<string> PreviewIds { get; set; } = [];
}