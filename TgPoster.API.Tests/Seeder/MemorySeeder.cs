using Microsoft.Extensions.Caching.Memory;
using TgPoster.API.Domain.Services;
using TgPoster.API.Tests.Helper;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;

namespace TgPoster.API.Tests.Seeder;

internal class MemorySeeder(PosterContext context) : BaseSeeder
{
	public override async Task Seed()
	{
		var file = new MessageFile
		{
			MessageId = GlobalConst.MessageId,
			FileType = FileTypes.NoOne,
			TgFileId = "afasfdasf",
			ContentType = "text/plain",
			Id = GlobalConst.FileId
		};
		
		await context.MessageFiles.AddRangeAsync(file);
	}
}