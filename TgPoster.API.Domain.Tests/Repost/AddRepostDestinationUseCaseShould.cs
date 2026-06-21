using Moq;
using Shared.Enums;
using Shouldly;
using TgPoster.API.Domain.UseCases.Repost.AddRepostDestination;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;

namespace TgPoster.API.Domain.Tests.Repost;

public class AddRepostDestinationUseCaseShould
{
	private readonly Mock<ITelegramChatService> chatService;
	private readonly Guid discoveredId = Guid.NewGuid();
	private readonly Guid sessionId = Guid.NewGuid();
	private readonly Mock<IAddRepostDestinationStorage> storage;
	private readonly AddRepostDestinationUseCase sut;

	public AddRepostDestinationUseCaseShould()
	{
		storage = new Mock<IAddRepostDestinationStorage>();
		chatService = new Mock<ITelegramChatService>();

		storage.Setup(s => s.GetTelegramSessionIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(sessionId);

		storage.Setup(s => s.UpsertDiscoveredChannelAsync(
				It.IsAny<long>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(),
				It.IsAny<ChatType>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(discoveredId);

		sut = new AddRepostDestinationUseCase(storage.Object, chatService.Object);
	}

	[Fact]
	public async Task ThrowRepostSettingsNotFoundException_WhenSettingsMissing()
	{
		storage.Setup(s => s.GetTelegramSessionIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((Guid?)null);

		var command = new AddRepostDestinationCommand(Guid.NewGuid(), "@channel");

		await Should.ThrowAsync<RepostSettingsNotFoundException>(async () =>
			await sut.Handle(command, CancellationToken.None));
	}

	[Fact]
	public async Task ThrowNoWritePermission_WhenCannotSendMessages()
	{
		SetupChat(false, false);

		var command = new AddRepostDestinationCommand(Guid.NewGuid(), "@channel");

		await Should.ThrowAsync<TelegramChatNoWritePermissionException>(async () =>
			await sut.Handle(command, CancellationToken.None));

		VerifyAddDestinationNeverCalled();
	}

	[Fact]
	public async Task ThrowNoMediaPermission_WhenCannotSendMedia()
	{
		SetupChat(true, false);

		var command = new AddRepostDestinationCommand(Guid.NewGuid(), "@channel");

		await Should.ThrowAsync<TelegramChatNoMediaPermissionException>(async () =>
			await sut.Handle(command, CancellationToken.None));

		VerifyAddDestinationNeverCalled();
	}

	[Fact]
	public async Task RecordDiscoverStatus_EvenWhenBlocked()
	{
		var info = SetupChat(false, false);

		var command = new AddRepostDestinationCommand(Guid.NewGuid(), "@channel");

		await Should.ThrowAsync<TelegramChatNoWritePermissionException>(async () =>
			await sut.Handle(command, CancellationToken.None));

		storage.Verify(s => s.UpsertDiscoveredChannelAsync(
			info.Id, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(),
			It.IsAny<ChatType>(), false, false, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task AddDestinationAndReturnBothIds_WhenPermissionsValid()
	{
		SetupChat(true, true);

		var destinationId = Guid.NewGuid();
		storage.Setup(s => s.AddDestinationAsync(
				It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string?>(), It.IsAny<string?>(),
				It.IsAny<int?>(), It.IsAny<ChatType>(), It.IsAny<ChatStatus>(), It.IsAny<string?>(),
				It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(destinationId);

		var command = new AddRepostDestinationCommand(Guid.NewGuid(), "@channel");

		var response = await sut.Handle(command, CancellationToken.None);

		response.Id.ShouldBe(destinationId);
		response.DiscoveredChannelId.ShouldBe(discoveredId);
		storage.Verify(s => s.AddDestinationAsync(
			It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string?>(), It.IsAny<string?>(),
			It.IsAny<int?>(), It.IsAny<ChatType>(), It.IsAny<ChatStatus>(), It.IsAny<string?>(),
			discoveredId, It.IsAny<CancellationToken>()), Times.Once);
	}

	private TelegramChatInfo SetupChat(bool canSendMessages, bool canSendMedia)
	{
		var info = new TelegramChatInfo
		{
			Id = 12345,
			AccessHash = 999,
			Title = "Test Channel",
			Username = "testchannel",
			IsChannel = true,
			IsGroup = false,
			CanSendMessages = canSendMessages,
			CanSendMedia = canSendMedia,
			Peer = TelegramPeer.Channel(12345, 999)
		};

		chatService.Setup(c => c.GetChatInfoAsync(sessionId, It.IsAny<string>(), It.IsAny<bool>()))
			.ReturnsAsync(info);
		chatService.Setup(c => c.GetFullChannelInfoAsync(sessionId, It.IsAny<TelegramChatInfo>()))
			.ReturnsAsync(new TelegramChannelInfoResult
			{
				Title = info.Title,
				Username = info.Username,
				MemberCount = 1000,
				IsChannel = true,
				IsGroup = false,
				AvatarThumbnail = null
			});

		return info;
	}

	private void VerifyAddDestinationNeverCalled() =>
		storage.Verify(s => s.AddDestinationAsync(
			It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string?>(), It.IsAny<string?>(),
			It.IsAny<int?>(), It.IsAny<ChatType>(), It.IsAny<ChatStatus>(), It.IsAny<string?>(),
			It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
}