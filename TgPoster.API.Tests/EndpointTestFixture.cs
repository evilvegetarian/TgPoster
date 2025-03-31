using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Security.Interfaces;
using Security.Models;
using Testcontainers.PostgreSql;
using TgPoster.Endpoint.Tests.Seeder;
using TgPoster.Storage.Data;

namespace TgPoster.Endpoint.Tests;

public class EndpointTestFixture : WebApplicationFactory<API.Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer dbContainer = new PostgreSqlBuilder().Build();
    private Mock<IIdentityProvider> mockIdentityProvider;

    public async Task InitializeAsync()
    {
        await dbContainer.StartAsync();
        var context = new PosterContext(new DbContextOptionsBuilder<PosterContext>()
            .UseNpgsql(dbContainer.GetConnectionString()).Options);
        await context.Database.MigrateAsync();
        await InsertSeed(context);
        await Mocked(context);
    }

    public new async Task DisposeAsync()
    {
        await dbContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("DataBase:ConnectionString",
                    dbContainer.GetConnectionString())
            }!)
            .Build();
        builder.UseConfiguration(configuration);
        builder.ConfigureLogging(cfg => cfg.ClearProviders());
        builder.ConfigureServices(services => { services.AddSingleton(mockIdentityProvider.Object); });
        base.ConfigureWebHost(builder);
    }

    private async Task Mocked(PosterContext context)
    {
        mockIdentityProvider = new Mock<IIdentityProvider>();
        mockIdentityProvider.Setup(ip => ip.Current)
            .Returns(new Identity(GlobalConst.Worked.UserId));
    }

    private async Task InsertSeed(PosterContext context)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettingsTest.json", false, true)
            .Build();
        var apiValue = configuration["Api"];

        var seeders = new BaseSeeder[]
        {
            new TelegramBotSeeder(context, apiValue),
            new MessageFileSeeder(context),
            new ScheduleSeeder(context),
            new MessageSeeder(context),
            new UserSeeder(context),
            new DaySeeder(context)
        };

        foreach (var seeder in seeders)
            await seeder.Seed();

        await context.SaveChangesAsync();
    }
}