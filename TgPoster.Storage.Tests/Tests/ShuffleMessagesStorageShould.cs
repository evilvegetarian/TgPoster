using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public class ShuffleMessagesStorageShould : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext _context;
	private readonly ShuffleMessagesStorage sut;

	public ShuffleMessagesStorageShould(StorageTestFixture fixture)
	{
		_context = fixture.GetDbContext();
		sut = new ShuffleMessagesStorage(_context);
	}

	[Fact]
	public async Task GetMessagesAsync_ShouldReturnOnlyFutureRegisterMessages()
	{
		var schedule = await new ScheduleBuilder(_context).CreateAsync();
		var future = await new MessageBuilder(_context)
			.WithSchedule(schedule)
			.WithStatus(MessageStatus.Register)
			.WithTimeMessage(DateTimeOffset.UtcNow.AddHours(1))
			.CreateAsync();
		await new MessageBuilder(_context)
			.WithSchedule(schedule)
			.WithStatus(MessageStatus.Register)
			.WithTimeMessage(DateTimeOffset.UtcNow.AddHours(-1))
			.CreateAsync();
		await new MessageBuilder(_context)
			.WithSchedule(schedule)
			.WithStatus(MessageStatus.Send)
			.WithTimeMessage(DateTimeOffset.UtcNow.AddHours(2))
			.CreateAsync();

		var result = await sut.GetMessagesAsync(schedule.Id, CancellationToken.None);

		result.Select(x => x.Id).ShouldBe([future.Id]);
	}

	[Fact]
	public async Task UpdateTimeAsync_ShouldAssignTimesByIndex()
	{
		var schedule = await new ScheduleBuilder(_context).CreateAsync();
		var messages = new List<Message>
		{
			await new MessageBuilder(_context).WithSchedule(schedule).CreateAsync(),
			await new MessageBuilder(_context).WithSchedule(schedule).CreateAsync(),
			await new MessageBuilder(_context).WithSchedule(schedule).CreateAsync()
		};
		var ids = messages.Select(x => x.Id).ToList();
		var times = new List<DateTimeOffset>
		{
			DateTimeOffset.UtcNow.AddMinutes(10),
			DateTimeOffset.UtcNow.AddMinutes(20),
			DateTimeOffset.UtcNow.AddMinutes(30)
		};

		await sut.UpdateTimeAsync(ids, times, CancellationToken.None);

		var updated = await _context.Messages
			.Where(x => ids.Contains(x.Id))
			.ToDictionaryAsync(x => x.Id, x => x.TimePosting);
		for (var i = 0; i < ids.Count; i++)
		{
			updated[ids[i]].ToString().ShouldBe(times[i].ToString());
		}
	}
}
