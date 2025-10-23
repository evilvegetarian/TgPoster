using MediatR;
using Security.Interfaces;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.Parse.DeleteParseChannel;

public sealed record DeleteParseChannelCommand(Guid Id) : IRequest;

internal sealed class DeleteParseChannelHandler(IDeleteParseChannelStorage storage, IIdentityProvider provider)
	: IRequestHandler<DeleteParseChannelCommand>
{
	public async Task Handle(DeleteParseChannelCommand request, CancellationToken cancellationToken)
	{
		var existParsing = await storage.ExistParseChannel(request.Id, provider.Current.UserId, cancellationToken);
		if (!existParsing)
		{
			throw new ParseChannelNotFoundException();
		}

		await storage.DeleteParseChannel(request.Id, cancellationToken);
	}
}