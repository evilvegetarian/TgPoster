using MediatR;
using Security.Interfaces;

namespace TgPoster.Domain.UseCases.TelegramBots.ListTelegramBot;

internal sealed class ListTelegramBotUseCase(IListTelegramBotStorage storage, IIdentityProvider identity)
    : IRequestHandler<ListTelegramBotQuery, List<TelegramBotResponse>>
{
    public async Task<List<TelegramBotResponse>> Handle(ListTelegramBotQuery request, CancellationToken cancellationToken)
    {
        var userId = identity.Current.UserId;
        var list = await storage.GetTelegramBotListAsync(userId, cancellationToken);
        return list;
    }
}