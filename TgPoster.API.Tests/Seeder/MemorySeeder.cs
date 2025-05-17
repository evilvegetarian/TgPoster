using Microsoft.Extensions.Caching.Memory;
using TgPoster.API.Domain.Services;

namespace TgPoster.Endpoint.Tests.Seeder;

internal class MemorySeeder(IMemoryCache memoryCache) : BaseSeeder
{
    public override async Task Seed()
    {
        var fileCacheItem = new FileCacheItem
        {
            Data = [1, 2, 3, 4],
            ContentType = "image/jpeg"
        };

        var memoryCacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };
        memoryCache.Set(GlobalConst.FileId, fileCacheItem, memoryCacheOptions);
    }
}