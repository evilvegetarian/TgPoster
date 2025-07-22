using MediatR;

namespace TgPoster.API.Domain.UseCases.Messages.ListMessage;

public sealed record ListMessageQuery(Guid ScheduleId, int PageNumber, int PageSize) : IRequest<PagedResponse<MessageResponse>>;