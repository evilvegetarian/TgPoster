using Microsoft.EntityFrameworkCore;
using Shared.Telegram;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Repositories;
using TgPoster.Storage.Tests.Builders;
using StorageTelegramSessionStatus = TgPoster.Storage.Data.Enum.TelegramSessionStatus;
using SharedTelegramSessionStatus = Shared.Telegram.TelegramSessionStatus;

namespace TgPoster.Storage.Tests.Tests;

public sealed class TelegramSessionRepositoryShould : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context;
	private readonly TelegramSessionRepository sut;

	public TelegramSessionRepositoryShould(StorageTestFixture fixture)
	{
		context = fixture.GetDbContext();
		sut = new(context);
	}

	[Fact]
	public async Task GetByIdAsync_WithValidSession_ShouldReturnSession()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var session = await new TelegramSessionBuilder(context)
			.WithUserId(user.Id)
			.CreateAsync();

		var result = await sut.GetByIdAsync(session.Id, CancellationToken.None);

		result.ShouldNotBeNull();
		result.Id.ShouldBe(session.Id);
		result.ApiId.ShouldBe(session.ApiId);
		result.ApiHash.ShouldBe(session.ApiHash);
		result.PhoneNumber.ShouldBe(session.PhoneNumber);
		result.IsActive.ShouldBe(session.IsActive);
		result.UserId.ShouldBe(user.Id);
	}

	[Fact]
	public async Task GetByIdAsync_WithNonExistentSession_ShouldReturnNull()
	{
		var result = await sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetByIdAsync_ShouldReturnSessionData()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var session = await new TelegramSessionBuilder(context)
			.WithUserId(user.Id)
			.WithSessionData("test_session_data_string")
			.CreateAsync();

		var result = await sut.GetByIdAsync(session.Id, CancellationToken.None);

		result.ShouldNotBeNull();
		result.SessionData.ShouldBe("test_session_data_string");
	}

	[Fact]
	public async Task UpdateSessionDataAsync_WithValidData_ShouldUpdateSessionData()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var session = await new TelegramSessionBuilder(context)
			.WithUserId(user.Id)
			.CreateAsync();
		var newSessionData = "updated_session_data";

		await sut.UpdateSessionDataAsync(session.Id, newSessionData, CancellationToken.None);
		var updated = await context.TelegramSessions
			.FirstAsync(s => s.Id == session.Id, CancellationToken.None);

		updated.SessionData.ShouldBe(newSessionData);
	}

	[Fact]
	public async Task UpdateSessionDataAsync_WithNonExistentSession_ShouldThrow()
	{
		await Should.ThrowAsync<InvalidOperationException>(async () =>
		{
			await sut.UpdateSessionDataAsync(Guid.NewGuid(), "data", CancellationToken.None);
		});
	}

	[Fact]
	public async Task UpdateStatusAsync_WithValidStatus_ShouldUpdateStatus()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var session = await new TelegramSessionBuilder(context)
			.WithUserId(user.Id)
			.WithStatus(StorageTelegramSessionStatus.AwaitingCode)
			.CreateAsync();

		await sut.UpdateStatusAsync(session.Id, SharedTelegramSessionStatus.Authorized, CancellationToken.None);
		var updated = await context.TelegramSessions
			.FirstAsync(s => s.Id == session.Id, CancellationToken.None);

		updated.Status.ShouldBe(StorageTelegramSessionStatus.Authorized);
	}

	[Fact]
	public async Task UpdateStatusAsync_WithDifferentStatuses_ShouldUpdateCorrectly()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var session = await new TelegramSessionBuilder(context)
			.WithUserId(user.Id)
			.CreateAsync();
context.ChangeTracker.Clear();
		await sut.UpdateStatusAsync(session.Id, SharedTelegramSessionStatus.CodeSent, CancellationToken.None);
		var updated1 = await context.TelegramSessions.FirstAsync(s => s.Id == session.Id, CancellationToken.None);
		updated1.Status.ShouldBe(StorageTelegramSessionStatus.CodeSent);
		await sut.UpdateStatusAsync(session.Id, SharedTelegramSessionStatus.AwaitingPassword, CancellationToken.None);
		var updated2 = await context.TelegramSessions.FirstAsync(s => s.Id == session.Id, CancellationToken.None);
		updated2.Status.ShouldBe(StorageTelegramSessionStatus.AwaitingPassword);
		await sut.UpdateStatusAsync(session.Id, SharedTelegramSessionStatus.Authorized, CancellationToken.None);
		var updated3 = await context.TelegramSessions.FirstAsync(s => s.Id == session.Id, CancellationToken.None);
		updated3.Status.ShouldBe(StorageTelegramSessionStatus.Authorized);
		await sut.UpdateStatusAsync(session.Id, SharedTelegramSessionStatus.Failed, CancellationToken.None);
		var updated4 = await context.TelegramSessions.FirstAsync(s => s.Id == session.Id, CancellationToken.None);
		updated4.Status.ShouldBe(StorageTelegramSessionStatus.Failed);
	}

	[Fact]
	public async Task UpdateStatusAsync_WithNonExistentSession_ShouldThrow()
	{
		await Should.ThrowAsync<InvalidOperationException>(async () =>
		{
			await sut.UpdateStatusAsync(Guid.NewGuid(), SharedTelegramSessionStatus.Authorized, CancellationToken.None);
		});
	}
}