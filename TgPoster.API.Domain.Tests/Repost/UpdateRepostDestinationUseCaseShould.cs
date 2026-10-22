using Moq;
using Security.IdentityServices;
using Shared.Enums;
using Shouldly;
using TgPoster.API.Domain.UseCases.Repost.UpdateRepostDestination;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;

namespace TgPoster.API.Domain.Tests.Repost;

public class UpdateRepostDestinationUseCaseShould
{
	private const long ChatId = 1375846471;
	private const string Username = "wc_world";

	private readonly Mock<ITelegramChatService> chatService = new();
	private readonly Guid id = Guid.NewGuid();
	private readonly Guid sessionId = Guid.NewGuid();
	private readonly Mock<IUpdateRepostDestinationStorage> storage = new();
	private readonly UpdateRepostDestinationUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public UpdateRepostDestinationUseCaseShould()
	{
		var identityProvider = new Mock<IIdentityProvider>();
		identityProvider.Setup(x => x.Current).Returns(new Identity(userId));

		sut = new UpdateRepostDestinationUseCase(storage.Object, chatService.Object, identityProvider.Object);
	}

	[Fact]
	public async Task UpdateDestination_WhenAllParametersAreValid()
	{
		SetupDestination(true, ChatStatus.Active);

		await sut.Handle(new UpdateRepostDestinationCommand(id, true, 10, 60, 1, 50, 100), CancellationToken.None);

		storage.Verify(s => s.UpdateDestinationAsync(
			id, true, ChatStatus.Active, 10, 60, 1, 50, 100, It.IsAny<CancellationToken>()), Times.Once);
		chatService.VerifyNoOtherCalls();
	}

	[Fact]
	public async Task ThrowRepostDestinationNotFoundException_WhenNotExists()
	{
		storage.Setup(s => s.GetDestinationAsync(It.IsAny<Guid>(), userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync((RepostDestinationInfo?)null);

		var command = new UpdateRepostDestinationCommand(Guid.NewGuid(), true, 0, 0, 1, 0, null);

		await Should.ThrowAsync<RepostDestinationNotFoundException>(async () =>
			await sut.Handle(command, CancellationToken.None));
	}

	[Fact]
	public async Task AllowNullMaxRepostsPerDay()
	{
		SetupDestination(true, ChatStatus.Active);

		await sut.Handle(new UpdateRepostDestinationCommand(id, true, 0, 0, 1, 0, null), CancellationToken.None);

		storage.Verify(s => s.UpdateDestinationAsync(
			id, true, ChatStatus.Active, 0, 0, 1, 0, null, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task NotCallTelegramAndKeepStatus_WhenDisabling()
	{
		SetupDestination(true, ChatStatus.Left);

		await sut.Handle(Disable(), CancellationToken.None);

		storage.Verify(s => s.UpdateDestinationAsync(
			id, false, ChatStatus.Left, 0, 0, 1, 0, null, It.IsAny<CancellationToken>()), Times.Once);
		chatService.VerifyNoOtherCalls();
	}

	[Fact]
	public async Task MarkActiveWithoutJoining_WhenAccountIsStillMember()
	{
		SetupDestination(false, ChatStatus.Left);
		SetupChat(ChatId.ToString(), false, TelegramOperationResult<TelegramChatInfo>.Success(ChatInfo(ChatId)));

		await sut.Handle(Enable(), CancellationToken.None);

		storage.Verify(s => s.UpdateDestinationAsync(
			id, true, ChatStatus.Active, 0, 0, 1, 0, null, It.IsAny<CancellationToken>()), Times.Once);
		chatService.Verify(c => c.TryGetChatInfoAsync(sessionId, Username, It.IsAny<bool>()), Times.Never);
	}

	[Fact]
	public async Task JoinByUsername_WhenAccountIsNotMember()
	{
		SetupDestination(false, ChatStatus.Left);
		SetupNotMember();
		SetupChat(Username, true, TelegramOperationResult<TelegramChatInfo>.Success(ChatInfo(ChatId)));

		await sut.Handle(Enable(), CancellationToken.None);

		chatService.Verify(c => c.TryGetChatInfoAsync(sessionId, Username, true), Times.Once);
		storage.Verify(s => s.UpdateDestinationAsync(
			id, true, ChatStatus.Active, 0, 0, 1, 0, null, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task Rejoin_WhenSavingActiveDestinationMarkedAsLeft()
	{
		SetupDestination(true, ChatStatus.Left);
		SetupNotMember();
		SetupChat(Username, true, TelegramOperationResult<TelegramChatInfo>.Success(ChatInfo(ChatId)));

		await sut.Handle(Enable(), CancellationToken.None);

		chatService.Verify(c => c.TryGetChatInfoAsync(sessionId, Username, true), Times.Once);
		storage.Verify(s => s.UpdateDestinationAsync(
			id, true, ChatStatus.Active, 0, 0, 1, 0, null, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ThrowAndNotEnable_WhenNotMemberAndChatHasNoUsername()
	{
		SetupDestination(false, ChatStatus.Left, null);
		SetupNotMember();

		await Should.ThrowAsync<RepostDestinationRejoinException>(async () =>
			await sut.Handle(Enable(), CancellationToken.None));

		VerifyNotUpdated();
	}

	[Fact]
	public async Task ThrowAndNotEnable_WhenJoinFails()
	{
		SetupDestination(false, ChatStatus.Left);
		SetupNotMember();
		SetupChat(Username, true, TelegramOperationResult<TelegramChatInfo>.Failed(
			TelegramOperationStatus.AccessDenied, "Не удалось вступить в чат: Заявка отправлена, ожидайте одобрения"));

		var exception = await Should.ThrowAsync<RepostDestinationRejoinException>(async () =>
			await sut.Handle(Enable(), CancellationToken.None));

		exception.Message.ShouldContain("Заявка отправлена");
		VerifyNotUpdated();
	}

	[Fact]
	public async Task ThrowAndNotEnable_WhenAccountIsBannedInChat()
	{
		SetupDestination(false, ChatStatus.Banned);
		SetupChat(ChatId.ToString(), false, TelegramOperationResult<TelegramChatInfo>.Failed(
			TelegramOperationStatus.ChannelBanned, "Чат заблокирован для этого пользователя"));

		await Should.ThrowAsync<RepostDestinationRejoinException>(async () =>
			await sut.Handle(Enable(), CancellationToken.None));

		chatService.Verify(c => c.TryGetChatInfoAsync(sessionId, Username, It.IsAny<bool>()), Times.Never);
		storage.Verify(s => s.AddAccountBanAsync(
			sessionId, ChatId, "Чат заблокирован для этого пользователя", It.IsAny<CancellationToken>()), Times.Once);
		VerifyNotUpdated();
	}

	[Fact]
	public async Task RecheckAccess_WhenActiveDestinationHasRecordedBan()
	{
		SetupDestination(true, ChatStatus.Active, isAccountBanned: true);
		SetupChat(ChatId.ToString(), false, TelegramOperationResult<TelegramChatInfo>.Success(ChatInfo(ChatId)));

		await sut.Handle(Enable(), CancellationToken.None);

		chatService.Verify(c => c.TryGetChatInfoAsync(sessionId, ChatId.ToString(), false), Times.Once);
		storage.Verify(s => s.RemoveAccountBanAsync(sessionId, ChatId, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task NeverStoreBannedStatus_WhenSavingDestinationWithRecordedBan()
	{
		SetupDestination(false, ChatStatus.Active, isAccountBanned: true);

		await sut.Handle(Disable(), CancellationToken.None);

		storage.Verify(s => s.UpdateDestinationAsync(
			id, false, ChatStatus.Active, 0, 0, 1, 0, null, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task RecordAccountBan_WhenRejoinFailsWithBan()
	{
		SetupDestination(false, ChatStatus.Left);
		SetupNotMember();
		SetupChat(Username, true, TelegramOperationResult<TelegramChatInfo>.Failed(
			TelegramOperationStatus.ChannelBanned, "CHANNEL_PRIVATE"));

		await Should.ThrowAsync<RepostDestinationRejoinException>(async () =>
			await sut.Handle(Enable(), CancellationToken.None));

		storage.Verify(s => s.AddAccountBanAsync(
			sessionId, ChatId, "CHANNEL_PRIVATE", It.IsAny<CancellationToken>()), Times.Once);
		storage.Verify(s => s.RemoveAccountBanAsync(
			It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task NotRecordAccountBan_WhenJoinFailsForOtherReason()
	{
		SetupDestination(false, ChatStatus.Left);
		SetupNotMember();
		SetupChat(Username, true, TelegramOperationResult<TelegramChatInfo>.Failed(
			TelegramOperationStatus.AccessDenied, "Заявка отправлена, ожидайте одобрения"));

		await Should.ThrowAsync<RepostDestinationRejoinException>(async () =>
			await sut.Handle(Enable(), CancellationToken.None));

		storage.Verify(s => s.AddAccountBanAsync(
			It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task RemoveAccountBan_WhenAccessIsConfirmed()
	{
		SetupDestination(false, ChatStatus.Banned);
		SetupChat(ChatId.ToString(), false, TelegramOperationResult<TelegramChatInfo>.Success(ChatInfo(ChatId)));

		await sut.Handle(Enable(), CancellationToken.None);

		storage.Verify(s => s.RemoveAccountBanAsync(sessionId, ChatId, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task RecordAccountBan_WhenAccountIsRestrictedInChat()
	{
		SetupDestination(false, ChatStatus.Banned);
		SetupChat(ChatId.ToString(), false, TelegramOperationResult<TelegramChatInfo>.Success(
			ChatInfo(ChatId, canSendMedia: false, isAccountRestricted: true)));

		await Should.ThrowAsync<TelegramChatNoMediaPermissionException>(async () =>
			await sut.Handle(Enable(), CancellationToken.None));

		storage.Verify(s => s.AddAccountBanAsync(
			sessionId, ChatId, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
		VerifyNotUpdated();
	}

	[Fact]
	public async Task KeepAccountBan_WhenChatForbidsMedia()
	{
		SetupDestination(false, ChatStatus.Banned);
		SetupChat(ChatId.ToString(), false,
			TelegramOperationResult<TelegramChatInfo>.Success(ChatInfo(ChatId, canSendMedia: false)));

		await Should.ThrowAsync<TelegramChatNoMediaPermissionException>(async () =>
			await sut.Handle(Enable(), CancellationToken.None));

		storage.Verify(s => s.RemoveAccountBanAsync(
			It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task ThrowAndNotEnable_WhenUsernameBelongsToAnotherChat()
	{
		SetupDestination(false, ChatStatus.Left);
		SetupNotMember();
		SetupChat(Username, true, TelegramOperationResult<TelegramChatInfo>.Success(ChatInfo(ChatId + 1)));

		await Should.ThrowAsync<RepostDestinationRejoinException>(async () =>
			await sut.Handle(Enable(), CancellationToken.None));

		VerifyNotUpdated();
	}

	[Fact]
	public async Task ThrowAndNotEnable_WhenAccountCannotSendMedia()
	{
		SetupDestination(false, ChatStatus.Banned);
		SetupChat(ChatId.ToString(), false,
			TelegramOperationResult<TelegramChatInfo>.Success(ChatInfo(ChatId, canSendMedia: false)));

		await Should.ThrowAsync<TelegramChatNoMediaPermissionException>(async () =>
			await sut.Handle(Enable(), CancellationToken.None));

		VerifyNotUpdated();
	}

	private UpdateRepostDestinationCommand Enable() => new(id, true, 0, 0, 1, 0, null);

	private UpdateRepostDestinationCommand Disable() => new(id, false, 0, 0, 1, 0, null);

	private void SetupDestination(
		bool isActive,
		ChatStatus chatStatus,
		string? username = Username,
		bool isAccountBanned = false
	) =>
		storage.Setup(s => s.GetDestinationAsync(id, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new RepostDestinationInfo(sessionId, ChatId, username, isActive, chatStatus, isAccountBanned));

	private void SetupNotMember() =>
		SetupChat(ChatId.ToString(), false, TelegramOperationResult<TelegramChatInfo>.Failed(
			TelegramOperationStatus.UsernameNotFound, "Чат не найден"));

	private void SetupChat(string input, bool autoJoin, TelegramOperationResult<TelegramChatInfo> result) =>
		chatService.Setup(c => c.TryGetChatInfoAsync(sessionId, input, autoJoin)).ReturnsAsync(result);

	private void VerifyNotUpdated() =>
		storage.Verify(s => s.UpdateDestinationAsync(
			It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<ChatStatus>(), It.IsAny<int>(), It.IsAny<int>(),
			It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);

	private static TelegramChatInfo ChatInfo(long chatId, bool canSendMedia = true, bool isAccountRestricted = false) =>
		new()
	{
		IsAccountRestricted = isAccountRestricted,
		Id = chatId,
		AccessHash = 1,
		Title = "Вебкам World",
		Username = Username,
		IsChannel = true,
		IsGroup = true,
		IsAdmin = false,
		IsCreator = false,
		CanSendMessages = true,
		CanSendMedia = canSendMedia,
		Peer = TelegramPeer.Channel(chatId, 1)
	};
}
