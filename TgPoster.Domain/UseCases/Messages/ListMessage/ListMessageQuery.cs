using MediatR;

namespace TgPoster.Domain.UseCases.Messages.ListMessage;

public sealed record ListMessageQuery(Guid ScheduleId) : IRequest<List<MessageResponse>>;