using MediatR;
using Shared.Telegram;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.Parse.RefreshParseChannelInfo;

internal sealed class RefreshParseChannelInfoUseCase(
	IRefreshParseChannelInfoStorage storage,
	ITelegramAuthService authService,
	ITelegramChatService chatService)
	: IRequestHandler<RefreshParseChannelInfoCommand>
{
	public async Task Handle(RefreshParseChannelInfoCommand request, CancellationToken ct)
	{
		var info = await storage.GetParseChannelInfoAsync(request.Id, ct);
		if (info is null)
			throw new ParseChannelNotFoundException();

		var (telegramSessionId, channel) = info.Value;
		var client = await authService.GetClientAsync(telegramSessionId, ct);
		var totalMessages = await chatService.GetChannelMessagesCountAsync(client, channel);

		await storage.UpdateTotalMessagesCountAsync(request.Id, totalMessages, ct);
	}
}
