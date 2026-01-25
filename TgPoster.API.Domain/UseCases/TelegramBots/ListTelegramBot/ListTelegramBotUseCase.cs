using MediatR;
using Security.IdentityServices;

namespace TgPoster.API.Domain.UseCases.TelegramBots.ListTelegramBot;

internal sealed class ListTelegramBotUseCase(
	IListTelegramBotStorage storage,
	IIdentityProvider identity)
	: IRequestHandler<ListTelegramBotQuery, TelegramBotListResponse>
{
	public async Task<TelegramBotListResponse> Handle(ListTelegramBotQuery request, CancellationToken ct)
	{
		var userId = identity.Current.UserId;
		var items = await storage.GetTelegramBotListAsync(userId, ct);
		return new TelegramBotListResponse { Items = items };
	}
}