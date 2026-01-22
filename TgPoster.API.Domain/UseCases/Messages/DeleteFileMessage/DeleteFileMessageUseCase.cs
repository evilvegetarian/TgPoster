using MediatR;
using Security.IdentityServices;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.Messages.DeleteFileMessage;

public class DeleteFileMessageUseCase(IDeleteFileMessageStorage storage, IIdentityProvider provider)
	: IRequestHandler<DeleteFileMessageCommand>
{
	public async Task Handle(DeleteFileMessageCommand request, CancellationToken cancellationToken)
	{
		var userId = provider.Current.UserId;
		var existMessage = await storage.ExistMessageAsync(request.Id, userId, cancellationToken);
		if (!existMessage)
		{
			throw new MessageNotFoundException(request.Id);
		}

		await storage.DeleteFileAsync(request.FileId, cancellationToken);
	}
}