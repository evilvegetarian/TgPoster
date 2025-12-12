using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public class UpdateAllTimeStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext _context = fixture.GetDbContext();
	private readonly UpdateAllTimeStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task UpdateTimeAsync_WithValidDate_ShouldCorrectUpdateTime()
	{
		var schedule = await new ScheduleBuilder(_context).CreateAsync();
		var messages = new List<Message>()
		{
			await new MessageBuilder(_context).WithSchedule(schedule).CreateAsync(),
			await new MessageBuilder(_context).WithSchedule(schedule).CreateAsync(),
			await new MessageBuilder(_context).WithSchedule(schedule).CreateAsync(),
		};
		var times = new List<DateTimeOffset>
		{
			DateTimeOffset.UtcNow.AddMinutes(1),
			DateTimeOffset.UtcNow.AddMinutes(2),
			DateTimeOffset.UtcNow.AddMinutes(10),
		};

		await sut.UpdateTimeAsync(messages.Select(x => x.Id).ToList(), times, CancellationToken.None);
		_context.ChangeTracker.Clear();
		var updatedMessages = await _context.Messages.Where(x => x.ScheduleId == schedule.Id).OrderBy(x=>x.TimePosting).ToListAsync();
		updatedMessages.Select(x => x.TimePosting.ToString()).ShouldBe(times.OrderBy(x=>x).Select(x => x.ToString()));
	}
}