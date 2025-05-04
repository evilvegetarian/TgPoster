using MediatR;
using Security.Interfaces;

namespace TgPoster.API.Domain.UseCases.TelegramBots.ListTelegramBot;

internal sealed class ListTelegramBotUseCase(IListTelegramBotStorage storage, IIdentityProvider identity)
    : IRequestHandler<ListTelegramBotQuery, List<TelegramBotResponse>>
{
    public async Task<List<TelegramBotResponse>> Handle(ListTelegramBotQuery request, CancellationToken ct)
    {
        var userId = identity.Current.UserId;
        var list = await storage.GetTelegramBotListAsync(userId, ct);
        return list;
    }
}