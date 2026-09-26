using Amazon.S3;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.MemoryStorage;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared;
using Shared.Services;
using TgPoster.Worker.Domain.ConfigModels;
using TgPoster.Worker.Domain.UseCases;
using TgPoster.Worker.Domain.UseCases.ClassifyChannel;
using TgPoster.Worker.Domain.UseCases.CleanupS3Files;
using TgPoster.Worker.Domain.UseCases.CommentRepostMonitor;
using TgPoster.Worker.Domain.UseCases.CrossPosting;
using TgPoster.Worker.Domain.UseCases.CrossPosting.Media;
using TgPoster.Worker.Domain.UseCases.CrossPosting.Publishing;
using TgPoster.Worker.Domain.UseCases.CrossPosting.Publishing.Bluesky;
using TgPoster.Worker.Domain.UseCases.DiscoverChannelLinks;
using TgPoster.Worker.Domain.UseCases.ImportRepostDestinations;
using TgPoster.Worker.Domain.UseCases.ParseChannel;
using TgPoster.Worker.Domain.UseCases.ParseChannelConsumer;
using TgPoster.Worker.Domain.UseCases.ParseChannelWorker;
using TgPoster.Worker.Domain.UseCases.ProcessMessageConsumer;
using TgPoster.Worker.Domain.UseCases.RepostMessageConsumer;
using TgPoster.Worker.Domain.UseCases.SendCommentConsumer;
using TgPoster.Worker.Domain.UseCases.SenderMessageWorker;
using TgPoster.Worker.Domain.UseCases.UpdateChannelStats;
using TgPoster.Worker.Domain.UseCases.WorkerJobStatus;

namespace TgPoster.Worker.Domain;

public static class DependencyInjection
{
	public static IServiceCollection AddDomain(this IServiceCollection services, IConfiguration configuration)
	{
		var telegramOptions = configuration.GetSection(nameof(TelegramOptions)).Get<TelegramOptions>()!;
		services.AddSingleton(telegramOptions);

		var repostImportOptions = configuration.GetSection(nameof(RepostImportOptions)).Get<RepostImportOptions>()
		                          ?? new RepostImportOptions();
		services.AddSingleton(repostImportOptions);

		var s3Options = configuration.GetSection(nameof(S3Options)).Get<S3Options>()!;
		services.AddSingleton(s3Options);
		services.AddSingleton<IAmazonS3>(_ =>
			new AmazonS3Client(s3Options.AccessKey, s3Options.SecretKey, new AmazonS3Config
			{
				ServiceURL = s3Options.ServiceUrl,
				ForcePathStyle = true
			}));

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
		services.AddScoped<ITelegramFileDownloader, TelegramFileDownloader>();
		services.AddScoped<ICrossPostMediaLoader, CrossPostMediaLoader>();
		services.TryAddSingleton(TimeProvider.System);
		services.AddScoped<ICrossPostPublisher, CrossPostPublisher>();
		services.AddMemoryCache();
		services.AddSingleton<BlueskySessionCache>();
		services.AddScoped<ISocialPublisher, BlueskyPublisher>();
		services.AddScoped<CrossPostWorker>();
		services.AddScoped<SenderMessageWorker>();
		services.AddScoped<ParseChannelWorker>();
		services.AddScoped<ParseChannelUseCase>();
		services.AddScoped<CommentRepostMonitorWorker>();
		services.AddScoped<TelegramExecuteServices>();
		services.AddScoped<DiscoverChannelLinksWorker>();
		services.AddScoped<ClassifyChannelWorker>();
		services.AddScoped<UpdateChannelStatsWorker>();
		services.AddScoped<CleanupS3FilesWorker>();
		services.AddScoped<ResumeRepostImportJobsWorker>();
		services.AddScoped<HangfireNextRunProvider>();

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
				opt.ConcurrentMessageLimit = 1;
			});
			x.AddConsumer<SendCommentConsumer>(opt =>
			{
				opt.ConcurrentMessageLimit = 1;
			});
			x.AddConsumer<ImportRepostDestinationsConsumer>(opt =>
			{
				opt.ConcurrentMessageLimit = 1;
			});
			//x.AddConsumer<ScrapeChannelConsumer>(opt =>
			//{
			//	opt.ConcurrentMessageLimit = 1;
			//});
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

		//Каждые 2 часов
		recurringJobManager.AddOrUpdate<DiscoverChannelLinksWorker>(
			WorkerJobNames.DiscoverChannelLinks,
			worker => worker.ProcessChannelsAsync(),
			"0 */2 * * *");

		recurringJobManager.AddOrUpdate<ClassifyChannelWorker>(
			"classify-channels-job",
			worker => worker.ClassifyChannelsAsync(),
			"*/20 * * * *");

		recurringJobManager.AddOrUpdate<UpdateChannelStatsWorker>(
			"update-channel-stats-job",
			worker => worker.UpdateStatsAsync(),
			Cron.Hourly());

		recurringJobManager.AddOrUpdate<CleanupS3FilesWorker>(
			WorkerJobNames.CleanupS3Files,
			worker => worker.CleanupAsync(),
			Cron.Weekly());

		recurringJobManager.AddOrUpdate<ResumeRepostImportJobsWorker>(
			"resume-repost-import-job",
			worker => worker.ResumeAsync(),
			Cron.Minutely());

		recurringJobManager.AddOrUpdate<CrossPostWorker>(
			"cross-post-job",
			worker => worker.ProcessAsync(),
			Cron.Minutely());

		var statusStorage = scope.ServiceProvider.GetRequiredService<IWorkerJobStatusStorage>();
		var nextRunProvider = scope.ServiceProvider.GetRequiredService<HangfireNextRunProvider>();
		statusStorage.EnsureRegisteredAsync(
				WorkerJobNames.DiscoverChannelLinks,
				nextRunProvider.GetNextRunAt(WorkerJobNames.DiscoverChannelLinks),
				CancellationToken.None)
			.GetAwaiter().GetResult();
	}

	private class AllowAllAuthorizationFilter : IDashboardAuthorizationFilter
	{
		public bool Authorize(DashboardContext context) => true;
	}
}