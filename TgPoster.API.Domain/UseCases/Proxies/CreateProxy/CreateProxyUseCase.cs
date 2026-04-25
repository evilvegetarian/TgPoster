using MediatR;
using Security.IdentityServices;
using Shared.Enums;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.Proxies.CreateProxy;

internal sealed class CreateProxyUseCase(
	ICreateProxyStorage storage,
	IIdentityProvider identityProvider
) : IRequestHandler<CreateProxyCommand, CreateProxyResponse>
{
	public async Task<CreateProxyResponse> Handle(CreateProxyCommand request, CancellationToken ct)
	{
		ProxyValidation.Validate(request.Type, request.Host, request.Port, request.Secret);

		var id = await storage.CreateAsync(
			identityProvider.Current.UserId,
			request.Name,
			request.Type,
			request.Host,
			request.Port,
			NormalizeOptional(request.Username),
			NormalizeOptional(request.Password),
			request.Type == ProxyType.MTProxy ? NormalizeOptional(request.Secret) : null,
			ct);

		return new CreateProxyResponse(id);
	}

	private static string? NormalizeOptional(string? value) =>
		string.IsNullOrWhiteSpace(value) ? null : value;
}
