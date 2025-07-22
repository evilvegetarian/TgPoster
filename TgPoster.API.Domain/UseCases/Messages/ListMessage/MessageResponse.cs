using MassTransit.Futures.Contracts;

namespace TgPoster.API.Domain.UseCases.Messages.ListMessage;

public sealed class MessageResponse
{
    public required Guid Id { get; set; }
    public string? TextMessage { get; set; }
    public DateTimeOffset TimePosting { get; set; }
    public required Guid ScheduleId { get; set; }

    public required bool NeedApprove { get; set; }

    public required bool CanApprove { get; set; }
    public List<FileResponse> Files { get; set; } = [];
}