using Bogus;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Data.VO;

namespace TgPoster.Storage.Tests;

public class Helper(PosterContext context)
{
	private readonly Faker faker = new();

	public async Task<User> CreateUserAsync()
	{
		var user = new User
		{
			Id = Guid.NewGuid(),
			UserName = new UserName(faker.Random.String2(7)),
			PasswordHash = faker.Internet.Password(),
			Email = new Email(faker.Internet.Email())
		};
		await context.Users.AddAsync(user);
		await context.SaveChangesAsync();
		return user;
	}

	public async Task<Schedule> CreateScheduleAsync()
	{
		var user = await CreateUserAsync();
		var telegramBot = await CreateTelegramBotAsync(user.Id);
		var schedule = new Schedule
		{
			Id = Guid.NewGuid(),
			Name = faker.Internet.UserName(),
			UserId = user.Id,
			TelegramBotId = telegramBot.Id,
			ChannelId = faker.Random.Long(-100000000000000, -199999999999999),
			ChannelName = faker.Company.CompanyName(),
			IsActive = true
		};
		await context.Schedules.AddAsync(schedule);
		await context.SaveChangesAsync();
		return schedule;
	}

	public async Task<Day> CreateDayAsync()
	{
		var schedule = await CreateScheduleAsync();
		return await CreateDayAsync(schedule.Id);
	}

	public Task<Day> CreateDayAsync(Guid scheduleId)
	{
		return CreateDayAsync(scheduleId, null);
	}

	public async Task<Day> CreateDayAsync(Guid scheduleId, DayOfWeek? dayOfWeek)
	{
		var randomTimes = Enumerable.Range(0, 10)
			.Select(_ => TimeOnly.FromDateTime(faker.Date.Between(
				new DateTime(1, 1, 1, 0, 0, 0),
				new DateTime(1, 1, 1, 23, 59, 59)
			))).ToList();
		var day = new Day
		{
			Id = Guid.NewGuid(),
			ScheduleId = scheduleId,
			DayOfWeek = dayOfWeek ?? faker.Random.Enum<DayOfWeek>(),
			TimePostings = randomTimes
		};
		await context.Days.AddAsync(day);
		await context.SaveChangesAsync();
		return day;
	}

	public async Task<TelegramBot> CreateTelegramBotAsync(Guid userId)
	{
		var bot = new TelegramBot
		{
			Id = Guid.NewGuid(),
			ApiTelegram = faker.Company.CompanyName(),
			ChatId = faker.Random.Long(),
			OwnerId = userId,
			Name = faker.Internet.UserName()
		};
		await context.TelegramBots.AddAsync(bot);
		await context.SaveChangesAsync();
		return bot;
	}

	public async Task<TelegramBot> CreateTelegramBotAsync()
	{
		var user = await CreateUserAsync();
		return await CreateTelegramBotAsync(user.Id);
	}

	public async Task<ChannelParsingParameters> CreateChannelParsingParametersAsync(
		Guid? scheduleId = null,
		ParsingStatus status = ParsingStatus.New,
		bool checkNewPosts = false
	)
	{
		var id = Guid.NewGuid();
		var schedule = scheduleId.HasValue
			? await context.Schedules.FindAsync(scheduleId.Value)
			: await CreateScheduleAsync();

		var cpp = new ChannelParsingParameters
		{
			Id = id,
			AvoidWords = ["spam", "ban"],
			Channel = "TestChannel",
			DeleteMedia = true,
			DeleteText = false,
			DateFrom = DateTime.UtcNow.AddDays(-1),
			LastParseId = 123,
			DateTo = DateTime.UtcNow.AddDays(1),
			NeedVerifiedPosts = true,
			ScheduleId = schedule!.Id,
			Status = status,
			CheckNewPosts = checkNewPosts
		};

		await context.ChannelParsingParameters.AddAsync(cpp);
		await context.SaveChangesAsync();
		return cpp;
	}

	public async Task<Message> CreateMessageAsync()
	{
		var schedule = await CreateScheduleAsync();
		return await CreateMessageAsync(schedule.Id);
	}

	public async Task<Message> CreateMessageAsync(Guid scheduleId)
	{
		var message = new Message
		{
			Id = Guid.NewGuid(),
			ScheduleId = scheduleId,
			TextMessage = faker.Lorem.Sentence(),
			TimePosting = DateTimeOffset.UtcNow.AddHours(faker.Random.Int(1, 24)),
			IsTextMessage = true,
			Status = MessageStatus.Register
		};
		await context.Messages.AddAsync(message);
		await context.SaveChangesAsync();
		return message;
	}

	public async Task<MessageFile> CreateMessageFileAsync(Guid messageId, string contentType = "image/jpeg")
	{
		var messageFile = new MessageFile
		{
			Id = Guid.NewGuid(),
			MessageId = messageId,
			ContentType = contentType,
			TgFileId = faker.Random.AlphaNumeric(20)
		};
		await context.MessageFiles.AddAsync(messageFile);
		await context.SaveChangesAsync();
		return messageFile;
	}

	public async Task<VideoMessageFile> CreateVideoMessageFileAsync(Guid messageId)
	{
		var videoFile = new VideoMessageFile
		{
			Id = Guid.NewGuid(),
			MessageId = messageId,
			ContentType = "video/mp4",
			TgFileId = faker.Random.AlphaNumeric(20),
			ThumbnailIds = [faker.Random.AlphaNumeric(15), faker.Random.AlphaNumeric(15)]
		};
		await context.MessageFiles.AddAsync(videoFile);
		await context.SaveChangesAsync();
		return videoFile;
	}

	public async Task<RefreshSession> CreateRefreshSessionAsync()
	{
		var user = await CreateUserAsync();
		return await CreateRefreshSessionAsync(user.Id);
	}

	public async Task<RefreshSession> CreateRefreshSessionAsync(Guid userId)
	{
		var refreshSession = new RefreshSession
		{
			Id = Guid.NewGuid(),
			UserId = userId,
			RefreshToken = Guid.NewGuid(),
			ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
		};
		await context.RefreshSessions.AddAsync(refreshSession);
		await context.SaveChangesAsync();
		return refreshSession;
	}
}