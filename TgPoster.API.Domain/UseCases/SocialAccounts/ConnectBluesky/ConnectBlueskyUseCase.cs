using MediatR;
using Security.Cryptography;
using Security.IdentityServices;
using Shared.Enums;
using Shared.Social.Bluesky;
using TgPoster.API.Domain.ConfigModels;
using TgPoster.Exceptions.BadRequest;

namespace TgPoster.API.Domain.UseCases.SocialAccounts.ConnectBluesky;

/// <summary>
///     Use case подключения аккаунта Bluesky
/// </summary>
internal sealed class ConnectBlueskyUseCase(
	IConnectBlueskyStorage storage,
	IIdentityProvider identity,
	IBlueskyClient blueskyClient,
	ICryptoAES crypto,
	TelegramOptions telegramOptions) : IRequestHandler<ConnectBlueskyCommand, ConnectSocialAccountResponse>
{
	public async Task<ConnectSocialAccountResponse> Handle(ConnectBlueskyCommand request, CancellationToken ct)
	{
		var handle = request.Handle.Trim().TrimStart('@');
		var session = await blueskyClient.CreateSessionAsync(handle, request.AppPassword, ct);
		if (!session.IsSuccess)
		{
			throw new InvalidSocialCredentialsException("Bluesky");
		}

		var id = await storage.UpsertAsync(
			identity.Current.UserId,
			SocialPlatform.Bluesky,
			session.Value!.Handle,
			session.Value.Did,
			crypto.Encrypt(telegramOptions.SecretKey, request.AppPassword),
			null,
			ct);

		return new ConnectSocialAccountResponse { Id = id };
	}
}
