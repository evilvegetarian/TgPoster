using MediatR;
using TgPoster.Exceptions.NotFound;
using TgPoster.Telegram.Abstractions;

namespace TgPoster.API.Domain.UseCases.Parse.RefreshParseChannelInfo;

internal sealed class RefreshParseChannelInfoUseCase(
	IRefreshParseChannelInfoStorage storage,
	ITelegramChatService chatService)
	: IRequestHandler<RefreshParseChannelInfoCommand>
{
	public async Task Handle(RefreshParseChannelInfoCommand request, CancellationToken ct)
	{
		var info = await storage.GetParseChannelInfoAsync(request.Id, ct);
		if (info is null)
		{
			throw new ParseChannelNotFoundException();
		}

		var (telegramSessionId, channel) = info.Value;
		var totalMessages = await chatService.GetChannelMessagesCountAsync(telegramSessionId, channel);

		await storage.UpdateTotalMessagesCountAsync(request.Id, totalMessages, ct);
	}
}