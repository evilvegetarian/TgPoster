using MediatR;
using Shared.SharedException;

namespace TgPoster.API.Domain.UseCases.OpenRouterSetting.DeleteOpenRouterSetting;

public class DeleteOpenRouterSettingHandler(IDeleteOpenRouterSettingStorage storage)
	: IRequestHandler<DeleteOpenRouterSettingCommand>
{
	public async Task Handle(DeleteOpenRouterSettingCommand request, CancellationToken cancellationToken)
	{
		if (!await storage.ExistsAsync(request.Id, cancellationToken))
		{
			throw new OpenRouterNotFoundException(request.Id);
		}

		await storage.DeleteAsync(request.Id, cancellationToken);
	}
}