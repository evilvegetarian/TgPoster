using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.API.Domain.Services;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public class CreateMessagesFromFilesUseCaseStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext _context = fixture.GetDbContext();
	private readonly CreateMessagesFromFilesUseCaseStorage _sut = new(fixture.GetDbContext(), new GuidFactory());

	[Fact]
	public async Task GetTelegramBot_WithMatchingScheduleAndUser_ShouldReturnTelegramBotDto()
	{
		var telegramBot = new TelegramBotBuilder(_context).Create();
		var schedule = new ScheduleBuilder(_context).WithUserId(telegramBot.OwnerId).WithTelegramBotId(telegramBot.Id).Create();

		var result = await _sut.GetTelegramBotAsync(schedule.Id, schedule.UserId, CancellationToken.None);

		result.ShouldNotBeNull();
		result.ApiTelegram.ShouldBe(telegramBot.ApiTelegram);
		result.ChatId.ShouldBe(telegramBot.ChatId);
	}

	[Fact]
	public async Task GetTelegramBot_WithScheduleNotFoundOrUserMismatch_ShouldReturnNull()
	{
		var telegramBot = new TelegramBotBuilder(_context).Create();
		var schedule = new ScheduleBuilder(_context).WithTelegramBotId(telegramBot.Id).Create();

		var anotherUser = Guid.Parse("b498cffb-ec28-4c27-a757-3192e2064e38");
		var result = await _sut.GetTelegramBotAsync(schedule.Id, anotherUser, CancellationToken.None);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetExistMessageTimePosting_WithFutureAndRegisterMessage_ShouldReturnOnlyFutureRegisterMessages()
	{
		var schedule = new ScheduleBuilder(_context).Create();

		var futureTime1 = DateTimeOffset.UtcNow.AddHours(1);
		var futureTime2 = DateTimeOffset.UtcNow.AddHours(2);
		var futureTime3 = DateTimeOffset.UtcNow.AddHours(5);

		var pastTime1 = DateTimeOffset.UtcNow.AddHours(-1);
		var pastTime2 = DateTimeOffset.UtcNow.AddHours(-2);

		var messages = new List<Message>
		{
			new MessageBuilder(_context)
				.WithScheduleId(schedule.Id)
				.WithTimeMessage(futureTime1)
				.WithStatus(MessageStatus.Register)
				.Build(),
			new MessageBuilder(_context)
				.WithScheduleId(schedule.Id)
				.WithTimeMessage(futureTime2)
				.WithStatus(MessageStatus.Register)
				.Build(),
			new MessageBuilder(_context)
				.WithScheduleId(schedule.Id)
				.WithTimeMessage(futureTime3)
				.WithStatus(MessageStatus.Error)
				.Build(),
			new MessageBuilder(_context)
				.WithScheduleId(schedule.Id)
				.WithTimeMessage(pastTime1)
				.WithStatus(MessageStatus.Register)
				.Build(),
			new MessageBuilder(_context)
				.WithScheduleId(schedule.Id)
				.WithTimeMessage(pastTime2)
				.WithStatus(MessageStatus.Register)
				.Build()
		};

		await _context.Messages.AddRangeAsync(messages);
		await _context.SaveChangesAsync();

		var result = await _sut.GetExistMessageTimePostingAsync(schedule.Id, CancellationToken.None);
		result.Count.ShouldBe(2);
		result.Select(x=>x.ToString()).ShouldContain(futureTime1.ToString());
		result.Select(x=>x.ToString()).ShouldContain(futureTime2.ToString());
	}

	[Fact]
	public async Task GetScheduleTime_WithValidScheduleId_ShouldReturnDictionaryOfDayAndTimePostings()
	{
		var schedule = new ScheduleBuilder(_context).Create();

		var day1 = new Day
		{
			Id = Guid.Parse("94a850f1-cdaa-4ec5-a3da-e1d6351708bd"),
			ScheduleId = schedule.Id,
			DayOfWeek = DayOfWeek.Monday,
			TimePostings = new List<TimeOnly> { new(10, 0), new(14, 0) }
		};

		var day2 = new Day
		{
			Id = Guid.Parse("1c13de87-5724-4349-9979-552409c4795b"),
			ScheduleId = schedule.Id,
			DayOfWeek = DayOfWeek.Wednesday,
			TimePostings = new List<TimeOnly> { new(9, 30) }
		};

		await _context.Days.AddRangeAsync(day1, day2);
		await _context.SaveChangesAsync();

		var result = await _sut.GetScheduleTimeAsync(schedule.Id, CancellationToken.None);

		result.Count.ShouldBe(2);
		result.ShouldContainKey(day1.DayOfWeek);
		result[day1.DayOfWeek].ShouldBe(day1.TimePostings);
		result.ShouldContainKey(day2.DayOfWeek);
		result[day2.DayOfWeek].ShouldBe(day2.TimePostings);
	}

	[Fact]
	public async Task CreateMessages_WithValidData_ShouldCreateMessagesWithProperFiles()
	{
		var schedule = new ScheduleBuilder(_context).Create();

		var file1 = new MediaFileResult
		{
			FileId = "photo1",
			MimeType = "image/jpeg"
		};
		var file2 = new MediaFileResult
		{
			FileId = "video1",
			MimeType = "video/mp4",
			PreviewPhotoIds = ["thumb1", "thumb2"]
		};

		var postingTimes = new List<DateTimeOffset>
		{
			DateTimeOffset.UtcNow.AddHours(3),
			DateTimeOffset.UtcNow.AddHours(4)
		};

		await _sut.CreateMessagesAsync(schedule.Id, [file1, file2], postingTimes, CancellationToken.None);

		var messages = await _context.Messages
			.Include(x => x.MessageFiles)
			.Where(x => x.ScheduleId == schedule.Id)
			.ToListAsync();

		messages.Count.ShouldBe(2);

		messages.SelectMany(x => x.MessageFiles.Select(file => file.TgFileId)).ShouldContain(file1.FileId);
		messages.SelectMany(x => x.MessageFiles.Select(file => file.TgFileId)).ShouldContain(file2.FileId);
	}
}