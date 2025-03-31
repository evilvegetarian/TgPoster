using MediatR;

namespace TgPoster.API.Domain.UseCases.Messages.ListMessage;

public sealed record ListMessageQuery(Guid ScheduleId) : IRequest<List<MessageResponse>>;