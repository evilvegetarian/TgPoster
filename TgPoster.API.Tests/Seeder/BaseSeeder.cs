using TgPoster.Storage.Data;

namespace TgPoster.Endpoint.Tests.Seeder;

internal abstract class BaseSeeder(PosterContext context)
{
    public abstract Task Seed();
}