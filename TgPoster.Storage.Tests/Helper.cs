using Bogus;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Data.VO;

namespace TgPoster.Storage.Tests;

public sealed class Helper
{
	private static readonly ThreadLocal<Faker> threadLocalFaker = new(() => new Faker());
	private readonly PosterContext context;

	public Helper(PosterContext context)
	{
		this.context = context ?? throw new ArgumentNullException(nameof(context));
	}

	private Faker Faker => threadLocalFaker.Value!;

	public User BuildUser(Action<User>? configure = null)
	{
		var user = new User
		{
			Id = Guid.NewGuid(),
			UserName = new UserName(Faker.Random.String2(7)),
			PasswordHash = Faker.Internet.Password(),
			Email = new Email(Faker.Internet.Email())
		};

		configure?.Invoke(user);
		return user;
	}

	public Schedule BuildSchedule(Guid userId, Guid telegramBotId, Action<Schedule>? configure = null)
	{
		var schedule = new Schedule
		{
			Id = Guid.NewGuid(),
			Name = Faker.Internet.UserName(),
			UserId = userId,
			TelegramBotId = telegramBotId,
			ChannelId = Faker.Random.Long(-199_999_999_999_999, -100_000_000_000_000),
			ChannelName = Faker.Company.CompanyName(),
			IsActive = true
		};

		configure?.Invoke(schedule);
		return schedule;
	}

	public Day BuildDay(
		Guid scheduleId,
		DayOfWeek? dayOfWeek = null,
		IEnumerable<TimeOnly>? timePostings = null,
		Action<Day>? configure = null
	)
	{
		var times = timePostings?.ToList() ?? GenerateRandomTimes(10);

		var day = new Day
		{
			Id = Guid.NewGuid(),
			ScheduleId = scheduleId,
			DayOfWeek = dayOfWeek ?? Faker.Random.Enum<DayOfWeek>(),
			TimePostings = times
		};

		configure?.Invoke(day);
		return day;
	}

	public TelegramBot BuildTelegramBot(Guid ownerId, Action<TelegramBot>? configure = null)
	{
		var bot = new TelegramBot
		{
			Id = Guid.NewGuid(),
			ApiTelegram = Faker.Company.CompanyName(),
			ChatId = Faker.Random.Long(long.MinValue / 2, long.MaxValue / 2),
			OwnerId = ownerId,
			Name = Faker.Internet.UserName()
		};

		configure?.Invoke(bot);
		return bot;
	}

	public ChannelParsingSetting BuildChannelParsingSetting(
		Guid scheduleId,
		ParsingStatus status = ParsingStatus.New,
		bool checkNewPosts = false,
		Action<ChannelParsingSetting>? configure = null
	)
	{
		var setting = new ChannelParsingSetting
		{
			Id = Guid.NewGuid(),
			AvoidWords = ["spam", "ban"],
			Channel = $"channel_{Faker.Random.Int(1, 10_000)}",
			DeleteMedia = true,
			DeleteText = false,
			DateFrom = DateTime.UtcNow.AddDays(-Faker.Random.Int(1, 7)),
			DateTo = DateTime.UtcNow.AddDays(Faker.Random.Int(1, 7)),
			LastParseId = Faker.Random.Int(1, 1_000_000),
			NeedVerifiedPosts = true,
			ScheduleId = scheduleId,
			Status = status,
			CheckNewPosts = checkNewPosts,
			UseAiForPosts = true
		};

		configure?.Invoke(setting);
		return setting;
	}

	public Message BuildMessage(Guid scheduleId, Action<Message>? configure = null)
	{
		var message = new Message
		{
			Id = Guid.NewGuid(),
			ScheduleId = scheduleId,
			TextMessage = Faker.Lorem.Sentence(),
			TimePosting = DateTimeOffset.UtcNow.AddMinutes(Faker.Random.Int(15, 24 * 60)),
			IsTextMessage = true,
			Status = Faker.Random.Enum<MessageStatus>(),
			Created = Faker.Date.Between(DateTime.UtcNow, DateTime.UtcNow.AddMonths(2))
		};

		configure?.Invoke(message);
		return message;
	}

	public MessageFile BuildMessageFile(
		Guid messageId,
		string contentType = "image/jpeg",
		Action<MessageFile>? configure = null
	)
	{
		var file = new MessageFile
		{
			Id = Guid.NewGuid(),
			MessageId = messageId,
			ContentType = contentType,
			TgFileId = Faker.Random.AlphaNumeric(20),
			FileType = FileTypes.Photo
		};

		configure?.Invoke(file);
		return file;
	}

	public RefreshSession BuildRefreshSession(Guid userId, Action<RefreshSession>? configure = null)
	{
		var session = new RefreshSession
		{
			Id = Guid.NewGuid(),
			UserId = userId,
			RefreshToken = Guid.NewGuid(),
			ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
		};

		configure?.Invoke(session);
		return session;
	}

	public async Task<User> CreateUserAsync(
		Action<User>? configure = null,
		bool saveChanges = true,
		CancellationToken cancellationToken = default
	)
	{
		var user = BuildUser(configure);
		await context.Users.AddAsync(user, cancellationToken);

		if (saveChanges)
		{
			await context.SaveChangesAsync(cancellationToken);
		}

		return user;
	}

	public async Task<TelegramBot> CreateTelegramBotAsync(
		Guid? ownerId = null,
		Action<TelegramBot>? configure = null,
		bool saveChanges = true,
		CancellationToken cancellationToken = default
	)
	{
		User owner;
		if (ownerId.HasValue)
		{
			owner = await context.Users.FindAsync([ownerId.Value], cancellationToken)
			        ?? throw new InvalidOperationException($"User {ownerId.Value} not found.");
		}
		else
		{
			owner = await CreateUserAsync(saveChanges: false, cancellationToken: cancellationToken);
		}

		var bot = BuildTelegramBot(owner.Id, configure);
		await context.TelegramBots.AddAsync(bot, cancellationToken);

		if (saveChanges)
		{
			await context.SaveChangesAsync(cancellationToken);
		}

		return bot;
	}

	public async Task<Schedule> CreateScheduleAsync(
		Guid? userId = null,
		Guid? telegramBotId = null,
		Action<Schedule>? configure = null,
		bool saveChanges = true,
		CancellationToken cancellationToken = default
	)
	{
		User user;
		if (userId.HasValue)
		{
			user = await context.Users.FindAsync([userId.Value], cancellationToken)
			       ?? throw new InvalidOperationException($"User {userId.Value} not found.");
		}
		else
		{
			user = await CreateUserAsync(saveChanges: false, cancellationToken: cancellationToken);
		}

		TelegramBot bot;
		if (telegramBotId.HasValue)
		{
			bot = await context.TelegramBots.FindAsync([telegramBotId.Value], cancellationToken)
			      ?? throw new InvalidOperationException($"Telegram bot {telegramBotId.Value} not found.");
		}
		else
		{
			bot = BuildTelegramBot(user.Id);
			await context.TelegramBots.AddAsync(bot, cancellationToken);
		}

		var schedule = BuildSchedule(user.Id, bot.Id, configure);
		await context.Schedules.AddAsync(schedule, cancellationToken);

		if (saveChanges)
		{
			await context.SaveChangesAsync(cancellationToken);
		}

		return schedule;
	}

	public async Task<Day> CreateDayAsync(
		Guid? scheduleId = null,
		DayOfWeek? dayOfWeek = null,
		IEnumerable<TimeOnly>? timePostings = null,
		Action<Day>? configure = null,
		bool saveChanges = true,
		CancellationToken cancellationToken = default
	)
	{
		Schedule schedule;
		if (scheduleId.HasValue)
		{
			schedule = await context.Schedules.FindAsync([scheduleId.Value], cancellationToken)
			           ?? throw new InvalidOperationException($"Schedule {scheduleId.Value} not found.");
		}
		else
		{
			schedule = await CreateScheduleAsync(saveChanges: false, cancellationToken: cancellationToken);
		}

		var day = BuildDay(schedule.Id, dayOfWeek, timePostings, configure);
		await context.Days.AddAsync(day, cancellationToken);

		if (saveChanges)
		{
			await context.SaveChangesAsync(cancellationToken);
		}

		return day;
	}

	public async Task<ChannelParsingSetting> CreateChannelParsingParametersAsync(
		Guid? scheduleId = null,
		ParsingStatus status = ParsingStatus.New,
		bool checkNewPosts = false,
		Action<ChannelParsingSetting>? configure = null,
		bool saveChanges = true,
		CancellationToken cancellationToken = default
	)
	{
		Schedule schedule;
		if (scheduleId.HasValue)
		{
			schedule = await context.Schedules.FindAsync([scheduleId.Value], cancellationToken)
			           ?? throw new InvalidOperationException($"Schedule {scheduleId.Value} not found.");
		}
		else
		{
			schedule = await CreateScheduleAsync(saveChanges: false, cancellationToken: cancellationToken);
		}

		var setting = BuildChannelParsingSetting(schedule.Id, status, checkNewPosts, configure);
		setting.Schedule = schedule;

		await context.ChannelParsingParameters.AddAsync(setting, cancellationToken);

		if (saveChanges)
		{
			await context.SaveChangesAsync(cancellationToken);
		}

		return setting;
	}

	public async Task<Message> CreateMessageAsync(
		Guid? scheduleId = null,
		Action<Message>? configure = null,
		bool saveChanges = true,
		CancellationToken cancellationToken = default
	)
	{
		Schedule schedule;
		if (scheduleId.HasValue)
		{
			schedule = await context.Schedules.FindAsync([scheduleId.Value], cancellationToken)
			           ?? throw new InvalidOperationException($"Schedule {scheduleId.Value} not found.");
		}
		else
		{
			schedule = await CreateScheduleAsync(saveChanges: false, cancellationToken: cancellationToken);
		}

		var message = BuildMessage(schedule.Id, configure);
		await context.Messages.AddAsync(message, cancellationToken);

		if (saveChanges)
		{
			await context.SaveChangesAsync(cancellationToken);
		}

		return message;
	}

	public async Task<IReadOnlyList<Message>> CreateMessagesAsync(
		Guid scheduleId,
		int count,
		Action<Message>? configure = null,
		bool saveChanges = true,
		CancellationToken cancellationToken = default
	)
	{
		if (count <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(count), count, "Count must be greater than zero.");
		}

		var schedule = await context.Schedules.FindAsync([scheduleId], cancellationToken)
		               ?? throw new InvalidOperationException($"Schedule {scheduleId} not found.");

		var messages = Enumerable.Range(0, count)
			.Select(_ => BuildMessage(schedule.Id, configure))
			.ToList();

		await context.Messages.AddRangeAsync(messages, cancellationToken);

		if (saveChanges)
		{
			await context.SaveChangesAsync(cancellationToken);
		}

		return messages;
	}

	public async Task<MessageFile> CreateMessageFileAsync(
		Guid messageId,
		string contentType = "image/jpeg",
		Action<MessageFile>? configure = null,
		bool saveChanges = true,
		CancellationToken cancellationToken = default
	)
	{
		var message = await context.Messages.FindAsync([messageId], cancellationToken)
		              ?? throw new InvalidOperationException($"Message {messageId} not found.");

		var file = BuildMessageFile(message.Id, contentType, configure);
		await context.MessageFiles.AddAsync(file, cancellationToken);

		if (saveChanges)
		{
			await context.SaveChangesAsync(cancellationToken);
		}

		return file;
	}

	public async Task<RefreshSession> CreateRefreshSessionAsync(
		Guid? userId = null,
		Action<RefreshSession>? configure = null,
		bool saveChanges = true,
		CancellationToken cancellationToken = default
	)
	{
		User user;
		if (userId.HasValue)
		{
			user = await context.Users.FindAsync([userId.Value], cancellationToken)
			       ?? throw new InvalidOperationException($"User {userId.Value} not found.");
		}
		else
		{
			user = await CreateUserAsync(saveChanges: false, cancellationToken: cancellationToken);
		}

		var session = BuildRefreshSession(user.Id, configure);
		await context.RefreshSessions.AddAsync(session, cancellationToken);

		if (saveChanges)
		{
			await context.SaveChangesAsync(cancellationToken);
		}

		return session;
	}

	private List<TimeOnly> GenerateRandomTimes(int count)
	{
		return Enumerable.Range(0, count)
			.Select(_ => TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(Faker.Random.Int(0, 24 * 60 - 1))))
			.Distinct()
			.OrderBy(time => time)
			.ToList();
	}
}