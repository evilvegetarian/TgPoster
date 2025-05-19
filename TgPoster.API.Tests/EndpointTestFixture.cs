using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using TgPoster.API.Common;
using TgPoster.API.Models;
using TgPoster.Endpoint.Tests.Seeder;
using TgPoster.Storage.Data;

namespace TgPoster.Endpoint.Tests;

public class EndpointTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer dbContainer = new PostgreSqlBuilder()
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithDatabase("testdb")
        .Build();

    private IMemoryCache? memoryCache;
    public HttpClient AuthClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await dbContainer.StartAsync();
        var context = new PosterContext(new DbContextOptionsBuilder<PosterContext>()
            .UseNpgsql(dbContainer.GetConnectionString()).Options);
        await context.Database.MigrateAsync();
        memoryCache = new MemoryCache(new MemoryCacheOptions());
        await InsertSeed(context);
        await CreateAuthClient();
    }

    private async Task CreateAuthClient()
    {
        AuthClient = base.CreateClient();
        var response = await AuthClient.PostAsJsonAsync(Routes.Account.SignIn, new SignInRequest
        {
            Login = GlobalConst.Worked.UserName,
            Password = "string"
        });
        response.EnsureSuccessStatusCode();

        var setCookie = response.Headers.TryGetValues("Set-Cookie", out var values) ? values.FirstOrDefault() : null;
        if (setCookie != null)
        {
            AuthClient.DefaultRequestHeaders.Add("Cookie", setCookie);
        }
    }


    public new async Task DisposeAsync()
    {
        await dbContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataBase:ConnectionString"] = dbContainer.GetConnectionString()
            })
            .Build();
        builder.UseConfiguration(configuration);
        builder.ConfigureLogging(cfg => cfg.ClearProviders());
        builder.ConfigureServices(services =>
        {
            services.AddSingleton(memoryCache!);
        });
        base.ConfigureWebHost(builder);
    }

    private async Task InsertSeed(PosterContext context)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettingsTest.json", false, true)
            .Build();
        var apiValue = configuration["Api"]!;

        var hash = configuration["Hash"]!;
        var seeders = new BaseSeeder[]
        {
            new TelegramBotSeeder(context, apiValue),
            new MessageFileSeeder(context),
            new ScheduleSeeder(context),
            new MessageSeeder(context),
            new UserSeeder(context, hash),
            new DaySeeder(context),
            new MemorySeeder(memoryCache!)
        };

        foreach (var seeder in seeders)
        {
            await seeder.Seed();
        }

        await context.SaveChangesAsync();
    }
}