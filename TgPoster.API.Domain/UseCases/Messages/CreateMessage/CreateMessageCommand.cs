using MediatR;
using Microsoft.AspNetCore.Http;

namespace TgPoster.API.Domain.UseCases.Messages.CreateMessage;

public sealed record CreateMessageCommand(
    Guid ScheduleId,
    DateTimeOffset TimePosting,
    string? Text,
    List<IFormFile> Files
) : IRequest<CreateMessageResponse>;