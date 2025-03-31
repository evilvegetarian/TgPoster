using MediatR;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;

namespace TgPoster.API.Domain.UseCases.Messages.GetMessageById;

public sealed record GetMessageQuery(Guid Id) : IRequest<MessageResponse>;