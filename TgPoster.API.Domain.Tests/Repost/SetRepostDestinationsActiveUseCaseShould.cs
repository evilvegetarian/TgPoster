using Moq;
using Security.IdentityServices;
using Shared.Enums;
using Shouldly;
using TgPoster.API.Domain.UseCases.Repost.SetRepostDestinationsActive;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;

namespace TgPoster.API.Domain.Tests.Repost;

public sealed class SetRepostDestinationsActiveUseCaseShould
{
	private const long MemberChatId = -1001375846471;
	private const long NotMemberChatId = -1001111111111;
	private const long ReadOnlyChatId = -1002222222222;

	private readonly Mock<ITelegramChatService> chatService = new();
	private readonly Guid sessionId = Guid.NewGuid();
	private readonly Guid settingsId = Guid.NewGuid();
	private readonly Mock<ISetRepostDestinationsActiveStorage> storage = new();
	private readonly SetRepostDestinationsActiveUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public SetRepostDestinationsActiveUseCaseShould()
	{
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));

		storage.Setup(s => s.GetTelegramSessionIdAsync(settingsId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(sessionId);

		sut = new SetRepostDestinationsActiveUseCase(storage.Object, chatService.Object, identity.Object);
	}

	[Fact]
	public async Task ThrowRepostSettingsNotFoundException_WhenSettingsNotOwnedByUser()
	{
		storage.Setup(s => s.GetTelegramSessionIdAsync(settingsId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync((Guid?)null);

		await Should.ThrowAsync<RepostSettingsNotFoundException>(async () =>
			await sut.Handle(Command(true, Guid.NewGuid()), CancellationToken.None));

		VerifyNothingChanged();
	}

	[Fact]
	public async Task ThrowRepostDestinationNotFoundException_WhenDestinationBelongsToOtherSettings()
	{
		var known = Destination(MemberChatId, false);
		var foreignId = Guid.NewGuid();
		SetupDestinations(known);

		var exception = await Should.ThrowAsync<RepostDestinationNotFoundException>(async () =>
			await sut.Handle(Command(true, known.Id, foreignId), CancellationToken.None));

		exception.Message.ShouldContain(foreignId.ToString());
		VerifyNothingChanged();
	}

	[Fact]
	public async Task DeactivateOnlyActiveDestinations_WithoutTelegram()
	{
		var active = Destination(MemberChatId, true);
		var inactive = Destination(NotMemberChatId, false);
		SetupDestinations(active, inactive);

		var result = await sut.Handle(Command(false, active.Id, inactive.Id), CancellationToken.None);

		result.UpdatedCount.ShouldBe(1);
		result.NotMemberCount.ShouldBe(0);
		result.NoPermissionCount.ShouldBe(0);
		storage.Verify(s => s.DeactivateAsync(
			It.Is<List<Guid>>(ids => ids.SequenceEqual(new[] { active.Id })),
			It.IsAny<CancellationToken>()), Times.Once);
		chatService.VerifyNoOtherCalls();
	}

	[Fact]
	public async Task NotCallStorage_WhenAllDestinationsAlreadyDisabled()
	{
		var inactive = Destination(MemberChatId, false);
		SetupDestinations(inactive);

		var result = await sut.Handle(Command(false, inactive.Id), CancellationToken.None);

		result.UpdatedCount.ShouldBe(0);
		VerifyNothingChanged();
	}

	[Fact]
	public async Task ActivateOnlyChatsWhereAccountIsMemberWithPermissions()
	{
		var member = Destination(MemberChatId, false);
		var notMember = Destination(NotMemberChatId, false);
		var readOnly = Destination(ReadOnlyChatId, false);
		SetupDestinations(member, notMember, readOnly);
		SetupJoinedChats(ChatInfo(MemberChatId), ChatInfo(ReadOnlyChatId, false));

		var result = await sut.Handle(Command(true, member.Id, notMember.Id, readOnly.Id), CancellationToken.None);

		result.UpdatedCount.ShouldBe(1);
		result.NotMemberCount.ShouldBe(1);
		result.NoPermissionCount.ShouldBe(1);
		storage.Verify(s => s.ActivateAsync(
			It.Is<List<Guid>>(ids => ids.SequenceEqual(new[] { member.Id })),
			It.IsAny<CancellationToken>()), Times.Once);
		chatService.Verify(c => c.TryGetJoinedChatsAsync(sessionId), Times.Once);
		chatService.VerifyNoOtherCalls();
	}

	[Fact]
	public async Task RecheckActiveDestinationMarkedAsLeft()
	{
		var left = Destination(MemberChatId, true, ChatStatus.Left);
		SetupDestinations(left);
		SetupJoinedChats(ChatInfo(MemberChatId));

		var result = await sut.Handle(Command(true, left.Id), CancellationToken.None);

		result.UpdatedCount.ShouldBe(1);
		storage.Verify(s => s.ActivateAsync(
			It.Is<List<Guid>>(ids => ids.SequenceEqual(new[] { left.Id })),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task NotCallTelegram_WhenAllDestinationsAlreadyActive()
	{
		var active = Destination(MemberChatId, true);
		SetupDestinations(active);

		var result = await sut.Handle(Command(true, active.Id), CancellationToken.None);

		result.UpdatedCount.ShouldBe(0);
		chatService.VerifyNoOtherCalls();
		VerifyNothingChanged();
	}

	[Fact]
	public async Task ThrowAndNotActivate_WhenJoinedChatsUnavailable()
	{
		var inactive = Destination(MemberChatId, false);
		SetupDestinations(inactive);
		chatService.Setup(c => c.TryGetJoinedChatsAsync(sessionId))
			.ReturnsAsync(TelegramOperationResult<IReadOnlyDictionary<long, TelegramChatInfo>>.Failed(
				TelegramOperationStatus.FloodWait, "FLOOD_WAIT_30", 30));

		var exception = await Should.ThrowAsync<RepostDestinationsMembershipCheckException>(async () =>
			await sut.Handle(Command(true, inactive.Id), CancellationToken.None));

		exception.Message.ShouldContain("попробуйте позже");
		VerifyNothingChanged();
	}

	private SetRepostDestinationsActiveCommand Command(bool isActive, params Guid[] ids) =>
		new(settingsId, ids.ToList(), isActive);

	private static RepostDestinationState Destination(
		long chatId,
		bool isActive,
		ChatStatus chatStatus = ChatStatus.Active
	) => new(Guid.NewGuid(), chatId, isActive, chatStatus);

	private void SetupDestinations(params RepostDestinationState[] destinations) =>
		storage.Setup(s => s.GetDestinationsAsync(settingsId, It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(destinations.ToList());

	private void SetupJoinedChats(params TelegramChatInfo[] chats) =>
		chatService.Setup(c => c.TryGetJoinedChatsAsync(sessionId))
			.ReturnsAsync(TelegramOperationResult<IReadOnlyDictionary<long, TelegramChatInfo>>.Success(
				chats.ToDictionary(c => c.Id)));

	private void VerifyNothingChanged()
	{
		storage.Verify(s => s.ActivateAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
		storage.Verify(s => s.DeactivateAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	private static TelegramChatInfo ChatInfo(long chatId, bool canSend = true)
	{
		var rawId = TelegramChatId.ResolveRaw(chatId);
		return new TelegramChatInfo
		{
			Id = rawId,
			AccessHash = 1,
			Title = "Канал",
			IsChannel = true,
			CanSendMessages = canSend,
			CanSendMedia = canSend,
			Peer = TelegramPeer.Channel(rawId, 1)
		};
	}
}
