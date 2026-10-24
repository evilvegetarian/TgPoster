using Shouldly;
using TgPoster.Worker.Domain.UseCases.SenderMessageWorker;

namespace TgPoster.Worker.Domain.Tests.SenderMessage;

public sealed class SenderMessageWorkerShould
{
	[Fact]
	public void BuildPostFailedDetails_WithUtcTimeErrorAndPlainTextPreview()
	{
		var message = new MessageDto
		{
			Id = Guid.NewGuid(),
			TimePosting = new DateTimeOffset(2026, 10, 24, 18, 5, 0, TimeSpan.FromHours(3)),
			Message = "<b>Заголовок</b> &amp; текст"
		};

		var result = SenderMessageWorker.BuildPostFailedDetails(message, "400: Bad Request: message is too long");

		result.ShouldBe("Пост на 24.10 15:05 UTC не отправлен: 400: Bad Request: message is too long\nТекст: Заголовок & текст");
	}

	[Fact]
	public void BuildPostFailedDetails_WithTruncatedPreview_WhenTextLong()
	{
		var message = new MessageDto
		{
			Id = Guid.NewGuid(),
			TimePosting = new DateTimeOffset(2026, 10, 24, 15, 5, 0, TimeSpan.Zero),
			Message = new string('а', 200)
		};

		var result = SenderMessageWorker.BuildPostFailedDetails(message, "ошибка");

		var preview = result.Split('\n')[1];
		preview.ShouldBe("Текст: " + new string('а', 79) + "…");
	}

	[Fact]
	public void BuildPostFailedDetails_WithoutPreview_WhenPostHasNoText()
	{
		var message = new MessageDto
		{
			Id = Guid.NewGuid(),
			TimePosting = new DateTimeOffset(2026, 10, 24, 15, 5, 0, TimeSpan.Zero)
		};

		var result = SenderMessageWorker.BuildPostFailedDetails(message, "ошибка");

		result.ShouldBe("Пост на 24.10 15:05 UTC не отправлен: ошибка");
	}

	[Fact]
	public void BuildSentMessageIds_WithFirstAlbumMessageAsTextHolder_WhenTextIsCaption()
	{
		var result = SenderMessageWorker.BuildSentMessageIds([10, 11, 12], null);

		result.ShouldNotBeNull();
		result.FirstId.ShouldBe(10);
		result.AllIds.ShouldBe([10, 11, 12]);
		result.TextMessageId.ShouldBe(10);
	}

	[Fact]
	public void BuildSentMessageIds_WithSeparateTextAsHolder_WhenTextSentSeparately()
	{
		var result = SenderMessageWorker.BuildSentMessageIds([10, 11], 12);

		result.ShouldNotBeNull();
		result.FirstId.ShouldBe(10);
		result.AllIds.ShouldBe([10, 11, 12]);
		result.TextMessageId.ShouldBe(12);
	}

	[Fact]
	public void BuildSentMessageIds_WithPostAsTextHolder_WhenTextPost()
	{
		var result = SenderMessageWorker.BuildSentMessageIds([42], null);

		result.ShouldNotBeNull();
		result.FirstId.ShouldBe(42);
		result.AllIds.ShouldBe([42]);
		result.TextMessageId.ShouldBe(42);
	}

	[Fact]
	public void BuildSentMessageIds_ReturnNull_WhenTelegramReturnedNoIds()
	{
		var result = SenderMessageWorker.BuildSentMessageIds([], 12);

		result.ShouldBeNull();
	}
}
