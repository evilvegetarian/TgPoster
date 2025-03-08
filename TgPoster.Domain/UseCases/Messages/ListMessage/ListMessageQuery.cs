using System.Net;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using TgPoster.Domain.UseCases.Files;

namespace TgPoster.Domain.UseCases.Messages.ListMessage;

public sealed record ListMessageQuery(Guid ScheduleId) : IRequest<List<MessageResponse>>;