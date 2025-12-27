using Bogus;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Tests.Builders;

public class MessageBuilder(PosterContext context)
{
	private static readonly Faker faker = FakerProvider.Instance;

	private readonly Message message = new()
	{
		Id = Guid.NewGuid(),
		ScheduleId = new ScheduleBuilder(context).Create().Id,
		TextMessage = faker.Lorem.Sentence(),
		TimePosting = DateTimeOffset.UtcNow.AddMinutes(faker.Random.Int(15, 24 * 60)),
		IsTextMessage = faker.Random.Bool(),
		Status = faker.Random.Enum<MessageStatus>(),
		Created = faker.Date.Between(DateTime.UtcNow, DateTime.UtcNow.AddMonths(2)),
		MessageFiles = new List<MessageFile>()
	};

	public MessageBuilder WithTimeMessage(DateTimeOffset value)
	{
		message.TimePosting = value;
		return this;
	}

	public MessageBuilder WithScheduleId(Guid value)
	{
		message.ScheduleId = value;
		return this;
	}

	public Message Build() => message;

	public Message Create()
	{
		context.Messages.AddRange(message);
		context.SaveChanges();
		return message;
	}

	public MessageBuilder WithStatus(MessageStatus status)
	{
		message.Status = status;
		return this;
	}

	public MessageBuilder WithCreatedDate(DateTime value)
	{
		message.Created = value;
		return this;
	}

	public MessageBuilder WithSchedule(Schedule schedule)
	{
		message.Schedule = schedule;
		message.ScheduleId = schedule.Id;

		return this;
	}

	public MessageBuilder WithIsVerified(bool value)
	{
		message.IsVerified = value;
		return this;
	}

	public async Task<Message> CreateAsync()
	{
		await Task.Delay(TimeSpan.FromSeconds(1));
		await context.Messages.AddRangeAsync(message);
		await context.SaveChangesAsync();
		return message;
	}

	public MessageBuilder WithTextMessage(string value)
	{
		message.TextMessage = value;
		return this;
	}

	public MessageBuilder WithPhotoMessageFile()
	{
		message.MessageFiles.Add(new MessageFile
		{
			Id = Guid.NewGuid(),
			FileType = FileTypes.Photo,
			ContentType = FileTypes.Photo.GetContentType(),
			TgFileId = "fasfs",
			MessageId = message.Id,
		});
		return this;
	}

	public MessageBuilder WithVideoMessageFile()
	{
		var messageFileId = Guid.NewGuid();
		message.MessageFiles.Add(new MessageFile
		{
			Id = messageFileId,
			FileType = FileTypes.Video,
			ContentType = FileTypes.Video.GetContentType(),
			TgFileId = "fadsdssfs",
			MessageId = message.Id,
			Thumbnails = new List<FileThumbnail>
			{
				new()
				{
					Id = Guid.NewGuid(),
					TgFileId = "asfsafasfs",
					MessageFileId = messageFileId,
					ContentType = FileTypes.Photo.GetContentType()
				}
			}
		});
		return this;
	}
}