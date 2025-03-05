using MediatR;
using Microsoft.AspNetCore.Http;

namespace TgPoster.Domain.UseCases.Messages.CreateMessagesFromFiles;

public sealed record CreateMessagesFromFilesCommand(Guid ScheduleId, List<IFormFile> Files) : IRequest;