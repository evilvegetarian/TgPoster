using Moq;
using Security.Cryptography;
using Security.IdentityServices;
using Shared.Telegram;
using Shouldly;
using TgPoster.API.Domain.ConfigModels;
using TgPoster.API.Domain.UseCases.Schedules.CreateSchedule;
using TgPoster.API.Domain.UseCases.Schedules.UpdateSchedule;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.Tests.Schedule;

public sealed class ScheduleYouTubeAccountOwnershipShould : IDisposable
{
	private readonly Mock<ICryptoAES> aes = new();
	private readonly TelegramBotManager botManager = new();
	private readonly Mock<ICreateScheduleStorage> createStorage = new();
	private readonly Mock<IIdentityProvider> identity = new();
	private readonly TelegramOptions options = new() { SecretKey = "key" };
	private readonly Mock<IUpdateScheduleStorage> updateStorage = new();
	private readonly Guid userId = Guid.NewGuid();

	public ScheduleYouTubeAccountOwnershipShould()
	{
		identity.Setup(x => x.Current).Returns(new Identity(userId));
	}

	public void Dispose()
	{
		botManager.Dispose();
	}

	[Fact]
	public async Task CreateThrowsNotFound_WhenYouTubeAccountBelongsToAnotherUser()
	{
		var command = new CreateScheduleCommand("Канал", Guid.NewGuid(), "@channel", Guid.NewGuid());
		createStorage.Setup(x => x.GetApiTokenAsync(command.TelegramBotId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync("encrypted");
		var sut = new CreateScheduleUseCase(options, createStorage.Object, identity.Object, aes.Object, botManager);

		await Should.ThrowAsync<YouTubeAccountNotFoundException>(() => sut.Handle(command, CancellationToken.None));

		aes.VerifyNoOtherCalls();
		createStorage.Verify(x => x.CreateScheduleAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
			It.IsAny<long>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task UpdateThrowsNotFound_WhenYouTubeAccountBelongsToAnotherUser()
	{
		var command = new UpdateScheduleCommand(Guid.NewGuid(), "Канал", Guid.NewGuid(), null, null, null);
		var sut = new UpdateScheduleUseCase(updateStorage.Object, identity.Object, options, aes.Object, botManager);

		await Should.ThrowAsync<YouTubeAccountNotFoundException>(() => sut.Handle(command, CancellationToken.None));

		updateStorage.Verify(x => x.UpdateScheduleAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string?>(),
			It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<bool?>(),
			It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task UpdateSavesYouTubeAccount_WhenItBelongsToUser()
	{
		var command = new UpdateScheduleCommand(Guid.NewGuid(), "Канал", Guid.NewGuid(), null, null, null);
		updateStorage.Setup(x => x.YouTubeAccountExistsAsync(command.YouTubeAccountId!.Value, userId,
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		var sut = new UpdateScheduleUseCase(updateStorage.Object, identity.Object, options, aes.Object, botManager);

		await sut.Handle(command, CancellationToken.None);

		updateStorage.Verify(x => x.UpdateScheduleAsync(command.Id, userId, command.Name, command.YouTubeAccountId,
			null, null, null, It.IsAny<CancellationToken>()), Times.Once);
	}
}
