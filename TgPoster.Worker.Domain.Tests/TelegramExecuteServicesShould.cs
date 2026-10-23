using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using TgPoster.Worker.Domain.UseCases;

namespace TgPoster.Worker.Domain.Tests;

public sealed class TelegramExecuteServicesShould
{
	private readonly Mock<ITelegramBotClient> bot = new();
	private readonly TelegramExecuteServices sut = new(NullLogger<TelegramExecuteServices>.Instance);

	[Fact]
	public async Task ReturnErrorWithCode_WhenTelegramRejectsText()
	{
		bot.Setup(b => b.SendRequest(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new ApiRequestException("Forbidden: bot was kicked", 403));

		var result = await sut.SendTextAsync(bot.Object, 100, "text", CancellationToken.None);

		result.IsSuccess.ShouldBeFalse();
		result.Error.ShouldNotBeNull();
		result.Error.ShouldContain("403");
	}

	[Fact]
	public async Task ReturnErrorWithCode_WhenTelegramRejectsMediaGroup()
	{
		bot.Setup(b => b.SendRequest(It.IsAny<SendMediaGroupRequest>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new ApiRequestException("Forbidden: bot was kicked", 403));

		var result = await sut.SendMediaGroupAsync(bot.Object, 100, [new InputMediaPhoto("file-id")],
			CancellationToken.None);

		result.IsSuccess.ShouldBeFalse();
		result.Error.ShouldNotBeNull();
		result.Error.ShouldContain("403");
	}
}
