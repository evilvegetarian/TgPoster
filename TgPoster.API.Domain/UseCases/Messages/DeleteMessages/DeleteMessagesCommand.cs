using MediatR;
using Security.Interfaces;

namespace TgPoster.API.Domain.UseCases.Messages.DeleteMessages;

public record DeleteMessagesCommand(List<Guid> Ids) : IRequest;

public class DeleteMessagesCommandHandler(IDeleteMessagesStorage storage, IIdentityProvider provider)
	: IRequestHandler<DeleteMessagesCommand>
{
	public Task Handle(DeleteMessagesCommand request, CancellationToken ct)
	{
		var userId = provider.Current.UserId;
		return storage.DeleteMessagesAsync(request.Ids, userId, ct);
	}
}

public interface IDeleteMessagesStorage
{
	Task DeleteMessagesAsync(List<Guid> ids, Guid userId, CancellationToken ct);
}