using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Security;
using Security.Interfaces;
using Security.Models;
using Testcontainers.PostgreSql;
using TgPoster.Domain.ConfigModels;
using TgPoster.Endpoint.Tests.Seeder;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.VO;

namespace TgPoster.Endpoint.Tests;

public class EndpointTestFixture : WebApplicationFactory<API.Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer dbContainer = new PostgreSqlBuilder().Build();
    private Mock<IIdentityProvider> mockIdentityProvider;
    public User TestUser { get; set; }

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

    public async Task InitializeAsync()
    {
        await dbContainer.StartAsync();
        var context = new PosterContext(new DbContextOptionsBuilder<PosterContext>()
            .UseNpgsql(dbContainer.GetConnectionString()).Options);
        await context.Database.MigrateAsync();
        await InsertSeed(context);
        await Mocked(context);
    }

    private async Task Mocked(PosterContext context)
    {
        TestUser = await InitUser(context);
        mockIdentityProvider = new Mock<IIdentityProvider>();
        mockIdentityProvider.Setup(ip => ip.Current)
            .Returns(new Identity(TestUser.Id));
    }

    public new async Task DisposeAsync()
    {
        await dbContainer.DisposeAsync();
    }

    private async Task<User> InitUser(PosterContext context)
    {
        var user = new User
        {
            Id = Guid.Parse("b6cbe54a-21d2-44d5-bfcc-a9f93e3fc93c"),
            PasswordHash = "password",
            UserName = new UserName("newUserName")
        };
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();
        return user;
    }

    private async Task InsertSeed(PosterContext context)
    {
        var seeders = new BaseSeeder[]
        {
            new ScheduleSeeder(context),
            new UserSeeder(context),
            new TelegramBotSeeder(context)
        };

        foreach (var seeder in seeders)
            await seeder.Seed();

        await context.SaveChangesAsync();
    }
}