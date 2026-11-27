using Microsoft.AspNetCore.Http;
using Moq;
using Security.IdentityServices;
using Shouldly;
using TgPoster.API.Domain.UseCases.TelegramBots.ListTelegramBot;
using TgPoster.API.Domain.UseCases.TelegramSessions;
using TgPoster.API.Domain.UseCases.TelegramSessions.ImportTelegramSession;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;

namespace TgPoster.API.Domain.Tests.TelegramSessions;

public sealed class ImportTelegramSessionUseCaseShould
{
	private readonly Guid userId = Guid.NewGuid();
	private readonly Mock<IImportTelegramSessionStorage> storage = new();
	private readonly Mock<IListTelegramBotStorage> botStorage = new();
	private readonly Mock<ITelegramAuthService> authService = new();
	private readonly ImportTelegramSessionUseCase sut;

	public ImportTelegramSessionUseCaseShould()
	{
		var identity = new Mock<IIdentityProvider>();
		identity.SetupGet(x => x.Current).Returns(new Identity(userId));
		sut = new(storage.Object, botStorage.Object, identity.Object, authService.Object);
	}

	[Fact]
	public async Task ReuseApiCredentials_WhenImportingWithCredentialsSessionId()
	{
		var sourceSessionId = Guid.NewGuid();
		var createdId = Guid.NewGuid();
		var file = CreateSessionFile([1, 2, 3]);
		storage.Setup(x => x.GetApiCredentialsAsync(userId, sourceSessionId, CancellationToken.None))
			.ReturnsAsync(new TelegramApiCredentials("123456", "reused-api-hash"));
		authService.Setup(x => x.ImportSessionAsync("123456", "reused-api-hash", It.IsAny<MemoryStream>(),
				CancellationToken.None))
			.ReturnsAsync(new ImportSessionResult { Success = true, PhoneNumber = "+79990000001" });
		storage.Setup(x => x.CreateAsync(userId, "123456", "reused-api-hash", "+79990000001", "imported", "AQID", null,
				CancellationToken.None))
			.ReturnsAsync(createdId);

		var response = await sut.Handle(
			new(null, null, file, "imported", null, sourceSessionId),
			CancellationToken.None);

		response.Id.ShouldBe(createdId);
		response.PhoneNumber.ShouldBe("+79990000001");
		authService.Verify(
			x => x.ImportSessionAsync("123456", "reused-api-hash", It.IsAny<MemoryStream>(), CancellationToken.None),
			Times.Once);
		storage.VerifyAll();
	}

	[Fact]
	public async Task ThrowNotFound_WhenCredentialsSessionIsMissingOrForeign()
	{
		var missingId = Guid.NewGuid();
		var file = CreateSessionFile([1, 2, 3]);
		storage.Setup(x => x.GetApiCredentialsAsync(userId, missingId, CancellationToken.None))
			.ReturnsAsync((TelegramApiCredentials?)null);

		await Should.ThrowAsync<TelegramSessionEntityNotFoundException>(() => sut.Handle(
			new(null, null, file, "imported", null, missingId),
			CancellationToken.None));

		authService.VerifyNoOtherCalls();
		storage.Verify(x => x.GetApiCredentialsAsync(userId, missingId, CancellationToken.None), Times.Once);
		storage.VerifyNoOtherCalls();
	}

	[Theory]
	[InlineData(null, "hash")]
	[InlineData("123", null)]
	[InlineData("", "")]
	public async Task ThrowInvalidCredentials_WhenNeitherCredentialsNorSourceSessionProvided(
		string? apiId,
		string? apiHash
	)
	{
		var file = CreateSessionFile([1, 2, 3]);

		await Should.ThrowAsync<InvalidTelegramApiCredentialsException>(() => sut.Handle(
			new(apiId, apiHash, file, "imported", null),
			CancellationToken.None));

		authService.VerifyNoOtherCalls();
		storage.VerifyNoOtherCalls();
	}

	private static IFormFile CreateSessionFile(byte[] content)
	{
		var file = new Mock<IFormFile>();
		file.Setup(x => x.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
			.Returns((Stream target, CancellationToken ct) => target.WriteAsync(content, ct).AsTask());
		return file.Object;
	}
}
