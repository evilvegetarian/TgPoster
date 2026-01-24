using Bogus;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Tests.Builders;

public class MessageFileBuilder(PosterContext context)
{
	private static readonly Faker faker = FakerProvider.Instance;

	private readonly MessageFile file = new()
	{
		Id = Guid.NewGuid(),
		MessageId = new MessageBuilder(context).Create()
			.Id,
		ContentType = "image/jpeg",
		TgFileId = faker.Random.AlphaNumeric(20),
		FileType = FileTypes.Photo,
		ParentFileId = null,
		Order = 0
	};

	public MessageFileBuilder WithMessageId(Guid messageId)
	{
		file.MessageId = messageId;
		return this;
	}

	public MessageFileBuilder WithContentType(string contentType)
	{
		file.ContentType = contentType;
		return this;
	}

	public MessageFile Build() => file;

	public MessageFile Create()
	{
		context.MessageFiles.AddRange(file);
		context.SaveChanges();
		return file;
	}

	public async Task<MessageFile> CreateAsync(CancellationToken ct = default)
	{
		await context.MessageFiles.AddRangeAsync(file);
		await context.SaveChangesAsync(ct);
		return file;
	}
}