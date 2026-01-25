using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages.Repost;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class CreateRepostSettingsStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly CreateRepostSettingsStorage sut = new(fixture.GetDbContext(), new GuidFactory());

	[Fact]
	public async Task ScheduleExistsAsync_WithExistingSchedule_ShouldReturnTrue()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();

		var result = await sut.ScheduleExistsAsync(schedule.Id, CancellationToken.None);

		result.ShouldBeTrue();
	}

	[Fact]
	public async Task ScheduleExistsAsync_WithNonExistingSchedule_ShouldReturnFalse()
	{
		var nonExistingId = Guid.NewGuid();

		var result = await sut.ScheduleExistsAsync(nonExistingId, CancellationToken.None);

		result.ShouldBeFalse();
	}

	[Fact]
	public async Task TelegramSessionExistsAndActiveAsync_WithActiveSession_ShouldReturnTrue()
	{
		var session = await new TelegramSessionBuilder(context)
			.WithIsActive(true)
			.CreateAsync();

		var result = await sut.TelegramSessionExistsAndActiveAsync(session.Id, CancellationToken.None);

		result.ShouldBeTrue();
	}

	[Fact]
	public async Task TelegramSessionExistsAndActiveAsync_WithInactiveSession_ShouldReturnFalse()
	{
		var session = await new TelegramSessionBuilder(context)
			.WithIsActive(false)
			.CreateAsync();

		var result = await sut.TelegramSessionExistsAndActiveAsync(session.Id, CancellationToken.None);

		result.ShouldBeFalse();
	}

	[Fact]
	public async Task TelegramSessionExistsAndActiveAsync_WithNonExistingSession_ShouldReturnFalse()
	{
		var nonExistingId = Guid.NewGuid();

		var result = await sut.TelegramSessionExistsAndActiveAsync(nonExistingId, CancellationToken.None);

		result.ShouldBeFalse();
	}

	[Fact]
	public async Task RepostSettingsExistForScheduleAsync_WithExistingSettings_ShouldReturnTrue()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		await new RepostSettingsBuilder(context)
			.WithScheduleId(schedule.Id)
			.CreateAsync();

		var result = await sut.RepostSettingsExistForScheduleAsync(schedule.Id, CancellationToken.None);

		result.ShouldBeTrue();
	}

	[Fact]
	public async Task RepostSettingsExistForScheduleAsync_WithoutSettings_ShouldReturnFalse()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();

		var result = await sut.RepostSettingsExistForScheduleAsync(schedule.Id, CancellationToken.None);

		result.ShouldBeFalse();
	}

	[Fact]
	public async Task CreateRepostSettingsAsync_WithValidData_ShouldCreateSettings()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var destinations = new List<long>
		{
			5132513256,
			252352366
		};

		var response = await sut.CreateRepostSettingsAsync(
			schedule.Id,
			session.Id,
			destinations,
			CancellationToken.None);

		var createdSettings = await context.Set<Data.Entities.RepostSettings>()
			.Include(x => x.Destinations)
			.FirstOrDefaultAsync(x => x.Id == response);
		
		createdSettings.ShouldNotBeNull();
		createdSettings.Id.ShouldBe(response);
		createdSettings.ScheduleId.ShouldBe(schedule.Id);
		createdSettings.TelegramSessionId.ShouldBe(session.Id);
		createdSettings.Destinations.Count.ShouldBe(2);
		createdSettings.Destinations.ShouldContain(d => d.ChatId == destinations.First());
		createdSettings.Destinations.ShouldContain(d => d.ChatId == destinations.Last());
		createdSettings.IsActive.ShouldBeTrue();
		createdSettings.Destinations.Count.ShouldBe(2);
	}
}
