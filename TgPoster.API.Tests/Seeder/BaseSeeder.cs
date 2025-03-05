using TgPoster.Storage.Data;

namespace TgPoster.Endpoint.Tests.Seeder;

internal abstract class BaseSeeder()
{
    public abstract Task Seed();
}