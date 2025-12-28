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
		FileType = faker.Random.Enum<FileTypes>()
	};

	public async Task<MessageFile> CreateAsync(CancellationToken ct)
	{
		await context.MessageFiles.AddRangeAsync(file);
		await context.SaveChangesAsync(ct);
		return file;
	}
}