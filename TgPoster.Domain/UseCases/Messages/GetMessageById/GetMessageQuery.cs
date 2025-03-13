using MediatR;
using TgPoster.Domain.UseCases.Messages.ListMessage;

namespace TgPoster.Domain.UseCases.Messages.GetMessageById;

public sealed record GetMessageQuery(Guid Id) : IRequest<MessageResponse>;