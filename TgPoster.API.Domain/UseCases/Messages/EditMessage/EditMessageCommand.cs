using MediatR;
using Microsoft.AspNetCore.Http;

namespace TgPoster.API.Domain.UseCases.Messages.EditMessage;

public sealed record EditMessageCommand(
    Guid Id,
    Guid ScheduleId,
    DateTimeOffset TimePosting,
    string? Text,
    List<Guid> Files,
    List<IFormFile> NewFiles) : IRequest;