using Hangfire;
using Hangfire.Dashboard;
using Hangfire.MemoryStorage;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared;
using Shared.Services;
using TgPoster.Worker.Domain.ConfigModels;
using TgPoster.Worker.Domain.UseCases;
using TgPoster.Worker.Domain.UseCases.ParseChannel;
using TgPoster.Worker.Domain.UseCases.ParseChannelConsumer;
using TgPoster.Worker.Domain.UseCases.ParseChannelWorker;
using TgPoster.Worker.Domain.UseCases.ProcessMessageConsumer;
using TgPoster.Worker.Domain.UseCases.CommentRepostMonitor;
using TgPoster.Worker.Domain.UseCases.RepostMessageConsumer;
using TgPoster.Worker.Domain.UseCases.SendCommentConsumer;
using TgPoster.Worker.Domain.UseCases.SenderMessageWorker;

namespace TgPoster.Worker.Domain;

public static class DependencyInjection
{
	public static IServiceCollection AddDomain(this IServiceCollection services, IConfiguration configuration)
	{
		var telegramOptions = configuration.GetSection(nameof(TelegramOptions)).Get<TelegramOptions>()!;
		services.AddSingleton(telegramOptions);


		services.AddMassTransient(configuration);

		services.AddHangfire(cfg =>
		{
			cfg.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
				.UseSimpleAssemblyNameTypeSerializer()
				.UseRecommendedSerializerSettings()
				.UseMemoryStorage();
		});
		services.AddHangfireServer();
		services.AddShared();
		services.AddScoped<SenderMessageWorker>();
		services.AddScoped<ParseChannelWorker>();
		services.AddScoped<ParseChannelUseCase>();
		services.AddScoped<CommentRepostMonitorWorker>();
		services.AddScoped<TelegramExecuteServices>();

		return services;
	}


	private static void AddMassTransient(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddMassTransit(x =>
		{
			x.AddConsumer<ParseChannelConsumer>(opt =>
			{
				opt.ConcurrentMessageLimit = 1;
			});
			x.AddConsumer<ProcessMessageConsumer>(opt =>
			{
				opt.ConcurrentMessageLimit = 1;
			});
			x.AddConsumer<RepostMessageConsumer>(opt =>
			{
				opt.ConcurrentMessageLimit = 5;
			});
			x.AddConsumer<SendCommentConsumer>(opt =>
			{
				opt.ConcurrentMessageLimit = 3;
			});

			x.UsingPostgres((context, cfg) =>
			{
				cfg.ConfigureEndpoints(context);
			});
			var dataBase = configuration.GetSection(nameof(DataBase)).Get<DataBase>()!;
			x.ConfigureMassTransient(dataBase.ConnectionString);
			x.AddPostgresMigrationHostedService();
		});
	}

	public static void AddHangfire(this WebApplication app)
	{
		app.UseHangfireDashboard("/hangfire", new DashboardOptions
		{
			Authorization = [new AllowAllAuthorizationFilter()]
		});

		using var scope = app.Services.CreateScope();
		var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

		recurringJobManager.AddOrUpdate<SenderMessageWorker>(
			"process-sender-message-job",
			worker => worker.ProcessMessagesAsync(),
			Cron.Minutely());

		recurringJobManager.AddOrUpdate<ParseChannelWorker>(
			"process-parse-channel-job",
			worker => worker.ProcessMessagesAsync(),
			Cron.Daily());

		recurringJobManager.AddOrUpdate<CommentRepostMonitorWorker>(
			"comment-repost-monitor-job",
			worker => worker.CheckForNewPostsAsync(),
			Cron.Minutely());
	}

	public class AllowAllAuthorizationFilter : IDashboardAuthorizationFilter
	{
		public bool Authorize(DashboardContext context) => true;
	}
}