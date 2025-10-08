using MediatR;

namespace TgPoster.API.Domain.UseCases.Messages.ApproveMessages;

public record ApproveMessagesCommand(List<Guid> MessageIds) : IRequest;