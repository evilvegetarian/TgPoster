using Moq;
using Security.IdentityServices;
using Shouldly;
using TgPoster.API.Domain.ConfigModels;
using TgPoster.API.Domain.Services;
using TgPoster.API.Domain.UseCases.Messages.GetMessageById;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;

namespace TgPoster.API.Domain.Tests.Messages;

public sealed class GetMessageUseCaseShould
{
	private static readonly DateTimeOffset Now = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);

	private readonly Guid messageId = Guid.NewGuid();
	private readonly Mock<IGetMessageStorage> storage = new();
	private readonly GetMessageUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public GetMessageUseCaseShould()
	{
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));
		var s3Options = new S3Options
		{
			AccessKey = "access",
			SecretKey = "secret",
			ServiceUrl = "https://s3.local",
			BucketName = "bucket"
		};

		var clock = new FixedTimeProvider(Now);
		sut = new GetMessageUseCase(storage.Object, identity.Object, s3Options, clock,
			new FileAccessTokenService(new TelegramOptions { SecretKey = "test-signing-key" }, clock));
	}

	[Fact]
	public async Task AllowEditAndDeleteFromChannel_WhenPublishedWithKnownIdsWithinWindow()
	{
		var timePosting = Now.AddHours(-1);
		SetupMessage(Published(timePosting));

		var result = await sut.Handle(new GetMessageQuery(messageId), CancellationToken.None);

		result.CanEditPublishedText.ShouldBeTrue();
		result.CanDeleteFromChannel.ShouldBeTrue();
		result.DeleteFromChannelUntil.ShouldBe(timePosting.AddHours(48).AddMinutes(-5));
	}

	[Fact]
	public async Task ForbidDeleteFromChannelButKeepDeadline_WhenWindowPassed()
	{
		var timePosting = Now.AddHours(-48).AddMinutes(5);
		SetupMessage(Published(timePosting));

		var result = await sut.Handle(new GetMessageQuery(messageId), CancellationToken.None);

		result.CanEditPublishedText.ShouldBeTrue();
		result.CanDeleteFromChannel.ShouldBeFalse();
		result.DeleteFromChannelUntil.ShouldBe(Now);
	}

	[Fact]
	public async Task ForbidEditAndDeleteFromChannel_WhenPostPublishedBeforeIdsWereSaved()
	{
		var message = Published(Now.AddHours(-1));
		message.HasTelegramMessageIds = false;
		message.HasTelegramTextMessage = false;
		SetupMessage(message);

		var result = await sut.Handle(new GetMessageQuery(messageId), CancellationToken.None);

		result.CanEditPublishedText.ShouldBeFalse();
		result.CanDeleteFromChannel.ShouldBeFalse();
		result.DeleteFromChannelUntil.ShouldBeNull();
	}

	[Fact]
	public async Task ForbidEditAndDeleteFromChannel_WhenNotPublished()
	{
		var message = Published(Now.AddHours(1));
		message.IsSent = false;
		message.DeliveryStatus = MessageDeliveryStatus.Planned;
		message.HasTelegramMessageIds = false;
		message.HasTelegramTextMessage = false;
		SetupMessage(message);

		var result = await sut.Handle(new GetMessageQuery(messageId), CancellationToken.None);

		result.CanEditPublishedText.ShouldBeFalse();
		result.CanDeleteFromChannel.ShouldBeFalse();
		result.DeleteFromChannelUntil.ShouldBeNull();
	}

	private MessageDto Published(DateTimeOffset timePosting) =>
		new()
		{
			Id = messageId,
			ScheduleId = Guid.NewGuid(),
			TimePosting = timePosting,
			IsVerified = true,
			IsSent = true,
			DeliveryStatus = MessageDeliveryStatus.Sent,
			HasTelegramMessageIds = true,
			HasTelegramTextMessage = true
		};

	private void SetupMessage(MessageDto message)
	{
		storage.Setup(x => x.GetMessagesAsync(messageId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(message);
	}

	private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
	{
		public override DateTimeOffset GetUtcNow() => now;
	}
}
