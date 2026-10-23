using Shouldly;
using Telegram.Bot.Exceptions;
using TgPoster.Worker.Domain.UseCases;

namespace TgPoster.Worker.Domain.Tests;

public sealed class TelegramErrorClassifierShould
{
	[Fact]
	public void DetectAccessLost_WhenForbidden()
	{
		var ex = new ApiRequestException("Forbidden: bot was kicked from the channel chat", 403);

		var result = TelegramErrorClassifier.IsAccessLost(ex);

		result.ShouldBeTrue();
	}

	[Theory]
	[InlineData("Bad Request: chat not found")]
	[InlineData("Bad Request: need administrator rights in the channel chat")]
	[InlineData("Bad Request: not enough rights to send photos to the chat")]
	[InlineData("Bad Request: CHAT_WRITE_FORBIDDEN")]
	[InlineData("Bad Request: have no rights to send a message")]
	[InlineData("Bad Request: Chat Not Found")]
	public void DetectAccessLost_WhenBadRequestAboutRights(string message)
	{
		var ex = new ApiRequestException(message, 400);

		var result = TelegramErrorClassifier.IsAccessLost(ex);

		result.ShouldBeTrue();
	}

	[Theory]
	[InlineData("Bad Request: message is too long", 400)]
	[InlineData("Too Many Requests: retry after 30", 429)]
	[InlineData("Too Many Requests: chat not found", 429)]
	public void NotDetectAccessLost_WhenOtherError(string message, int errorCode)
	{
		var ex = new ApiRequestException(message, errorCode);

		var result = TelegramErrorClassifier.IsAccessLost(ex);

		result.ShouldBeFalse();
	}
}
