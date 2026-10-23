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
}
