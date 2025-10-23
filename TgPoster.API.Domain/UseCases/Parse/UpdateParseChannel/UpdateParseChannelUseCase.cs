using MediatR;
using Security.Interfaces;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.Parse.UpdateParseChannel;

internal class UpdateParseChannelUseCase(IIdentityProvider provider, IUpdateParseChannelStorage storage)
	: IRequestHandler<UpdateParseChannelCommand>
{
	public async Task Handle(UpdateParseChannelCommand request, CancellationToken cancellationToken)
	{
		var userId = provider.Current.UserId;
		if (!await storage.ExistParseChannelAsync(request.Id, userId, cancellationToken))
		{
			throw new ParseChannelNotFoundException();
		}


		await storage.UpdateParseChannelAsync(request, cancellationToken);
	}
}