using MediatR;
using Security.Interfaces;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.TelegramBots.DeleteTelegramBot;

public class DeleteTelegramUseCase(IDeleteTelegramBotStorage storage, IIdentityProvider identity)
	: IRequestHandler<DeleteTelegramCommand>
{
	public async Task Handle(DeleteTelegramCommand request, CancellationToken cancellationToken)
	{
		if (!await storage.ExistsAsync(request.Id, identity.Current.UserId, cancellationToken))
		{
			throw new TelegramBotNotFoundException(request.Id);
		}
		
		await storage.DeleteTelegramBotAsync(request.Id, identity.Current.UserId, cancellationToken);
	}
}