using MassTransit;
using MediatR;
using Shared.Telegram;
using Telegram.Bot;
using TgPoster.API.Domain.Extensions;
using TgPoster.API.Domain.Services;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;

namespace TgPoster.API.Domain.UseCases.Parse.CreateParseChannel;

internal class CreateParseChannelUseCase(
	IParseChannelStorage storage,
	TelegramTokenService tokenService,
	IBus bus,
	ITelegramChatService chatService,
	TelegramBotManager botManager)
	: IRequestHandler<CreateParseChannelCommand, CreateParseChannelResponse>
{
	public async Task<CreateParseChannelResponse> Handle(CreateParseChannelCommand request, CancellationToken ct)
	{
		var (token, _) = await tokenService.GetTokenByScheduleIdAsync(request.ScheduleId, ct);
		var bot = botManager.GetClient(token);
		var channel = request.Channel.ConvertToTelegramHandle();
		var chat = await bot.GetChat(channel, ct);

		var totalMessagesCount =
			await chatService.GetChannelMessagesCountAsync(request.TelegramSessionId, chat.Username!);

		var id = await storage.AddParseChannelParametersAsync(chat.Username!, request.AlwaysCheckNewPosts,
			request.ScheduleId, request.DeleteText, request.DeleteMedia, request.AvoidWords, request.NeedVerifiedPosts,
			request.DateFrom, request.DateTo, request.UseAiForPosts, request.TelegramSessionId, totalMessagesCount, ct);
		// await bus.Publish(new ParseChannelContract { Id = id }, ct);

		return new CreateParseChannelResponse
		{
			Id = id
		};
	}
}