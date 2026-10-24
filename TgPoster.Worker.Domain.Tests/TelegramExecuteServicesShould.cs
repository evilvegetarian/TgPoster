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

	[Fact]
	public async Task ReturnAllAlbumIds_WhenMediaGroupSent()
	{
		bot.Setup(b => b.SendRequest(It.IsAny<SendMediaGroupRequest>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([new Message { Id = 10 }, new Message { Id = 11 }, new Message { Id = 12 }]);

		var result = await sut.SendMediaGroupAsync(bot.Object, 100,
			[new InputMediaPhoto("file-1"), new InputMediaPhoto("file-2"), new InputMediaPhoto("file-3")],
			CancellationToken.None);

		result.IsSuccess.ShouldBeTrue();
		result.MessageIds.ShouldBe([10, 11, 12]);
	}

	[Fact]
	public async Task ReturnSingleId_WhenTextSent()
	{
		bot.Setup(b => b.SendRequest(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new Message { Id = 42 });

		var result = await sut.SendTextAsync(bot.Object, 100, "text", CancellationToken.None);

		result.IsSuccess.ShouldBeTrue();
		result.MessageIds.ShouldBe([42]);
	}

	[Fact]
	public async Task ReturnAccessLost_WhenBotLostRightsInChannel()
	{
		bot.Setup(b => b.SendRequest(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new ApiRequestException("Bad Request: need administrator rights in the channel chat", 400));

		var result = await sut.SendTextAsync(bot.Object, 100, "text", CancellationToken.None);

		result.IsSuccess.ShouldBeFalse();
		result.IsAccessLost.ShouldBeTrue();
	}

	[Fact]
	public async Task ReturnNotAccessLost_WhenTelegramRejectsContent()
	{
		bot.Setup(b => b.SendRequest(It.IsAny<SendMediaGroupRequest>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new ApiRequestException("Bad Request: message caption is too long", 400));

		var result = await sut.SendMediaGroupAsync(bot.Object, 100, [new InputMediaPhoto("file-id")],
			CancellationToken.None);

		result.IsSuccess.ShouldBeFalse();
		result.IsAccessLost.ShouldBeFalse();
	}
}
