using MediatR;
using Security.IdentityServices;
using TgPoster.Exceptions.BadRequest;

namespace TgPoster.API.Domain.UseCases.OpenRouterSetting.DeleteOpenRouterSetting;

public class DeleteOpenRouterSettingUseCase(IDeleteOpenRouterSettingStorage storage, IIdentityProvider provider)
	: IRequestHandler<DeleteOpenRouterSettingCommand>
{
	public async Task Handle(DeleteOpenRouterSettingCommand request, CancellationToken cancellationToken)
	{
		if (!await storage.ExistsAsync(request.Id, provider.Current.UserId, cancellationToken))
		{
			throw new OpenRouterNotFoundException(request.Id);
		}

		await storage.DeleteAsync(request.Id, cancellationToken);
	}
}