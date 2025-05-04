using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Tests;

public class StorageTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer dbContainer = new PostgreSqlBuilder().Build();

    public virtual async Task InitializeAsync()
    {
        await dbContainer.StartAsync();
        var forumDbContext = new PosterContext(new DbContextOptionsBuilder<PosterContext>()
            .UseNpgsql(dbContainer.GetConnectionString()).Options);
        await forumDbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await dbContainer.StopAsync();
    }

    public PosterContext GetDbContext()
    {
        return new PosterContext(new DbContextOptionsBuilder<PosterContext>()
            .UseNpgsql(dbContainer.GetConnectionString()).Options);
    }
}