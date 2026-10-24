using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shouldly;
using TgPoster.API.Domain.UseCases.Repost.GetRepostLogsSummary;
using TgPoster.API.Domain.UseCases.Repost.ListRepostLogs;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Storages.ChannelCleanup;
using TgPoster.Storage.Storages.CommentRepost;
using TgPoster.Storage.Storages.CrossPosting;
using TgPoster.Storage.Storages.Repost;
using TgPoster.Storage.Storages.SocialAccounts;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

/// <summary>
/// Каждый метод storage с проверкой владельца не отдаёт и не меняет данные чужого пользователя
/// </summary>
public sealed class ForeignUserAccessStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private static readonly CancellationToken Ct = CancellationToken.None;
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly Guid foreignUserId = Guid.NewGuid();

	[Fact]
	public async Task AddScheduleOpenRouter_ExistOpenRouterAsync_WithForeignUser_ShouldReturnFalse()
	{
		var setting = new OpenRouterSettingBuilder(context).Create();

		var result = await new AddScheduleOpenRouterSettingStorage(context)
			.ExistOpenRouterAsync(setting.Id, foreignUserId, Ct);

		result.ShouldBeFalse();
	}

	[Fact]
	public async Task ClassifierSettings_GetUserSessionIdsAsync_WithForeignUser_ShouldNotContainSession()
	{
		var session = new TelegramSessionBuilder(context).Create();

		var result = await new ClassifierSettingsStorage(context).GetUserSessionIdsAsync(foreignUserId, Ct);

		result.ShouldNotContain(session.Id);
	}

	[Fact]
	public async Task CreateDays_ScheduleExistAsync_WithForeignUser_ShouldReturnFalse()
	{
		var schedule = new ScheduleBuilder(context).Create();

		var result = await new CreateDaysStorage(context, new GuidFactory())
			.ScheduleExistAsync(schedule.Id, foreignUserId, Ct);

		result.ShouldBeFalse();
	}

	[Fact]
	public async Task CreateMessage_GetTelegramBotAsync_WithForeignUser_ShouldReturnNull()
	{
		var schedule = new ScheduleBuilder(context).Create();

		var result = await new CreateMessageStorage(context, new GuidFactory())
			.GetTelegramBotAsync(schedule.Id, foreignUserId, Ct);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task CreateMessagesFromFiles_GetTelegramBotAsync_WithForeignUser_ShouldReturnNull()
	{
		var schedule = new ScheduleBuilder(context).Create();

		var result = await new CreateMessagesFromFilesUseCaseStorage(context, new GuidFactory())
			.GetTelegramBotAsync(schedule.Id, foreignUserId, Ct);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task CreateSchedule_GetApiTokenAsync_WithForeignUser_ShouldReturnNull()
	{
		var bot = new TelegramBotBuilder(context).Create();

		var result = await new CreateScheduleStorage(context, new GuidFactory())
			.GetApiTokenAsync(bot.Id, foreignUserId, Ct);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task CreateSchedule_YouTubeAccountExistsAsync_ShouldReturnTrueOnlyForOwner()
	{
		var account = new YouTubeAccountBuilder(context).Create();
		var sut = new CreateScheduleStorage(context, new GuidFactory());

		var own = await sut.YouTubeAccountExistsAsync(account.Id, account.UserId, Ct);
		var foreign = await sut.YouTubeAccountExistsAsync(account.Id, foreignUserId, Ct);

		own.ShouldBeTrue();
		foreign.ShouldBeFalse();
	}

	[Fact]
	public async Task CrossPostTarget_SocialAccountExistsAsync_WithForeignUser_ShouldReturnFalse()
	{
		var account = new SocialAccountBuilder(context).Create();

		var result = await new CrossPostTargetStorage(context, new GuidFactory())
			.SocialAccountExistsAsync(account.Id, foreignUserId, Ct);

		result.ShouldBeFalse();
	}

	[Fact]
	public async Task CrossPostTarget_GetContextAsync_WithForeignUser_ShouldReturnNull()
	{
		var schedule = new ScheduleBuilder(context).Create();

		var result = await new CrossPostTargetStorage(context, new GuidFactory())
			.GetContextAsync(schedule.Id, foreignUserId, Ct);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task CrossPostTarget_GetAccountAsync_WithForeignUser_ShouldReturnNull()
	{
		var account = new SocialAccountBuilder(context).Create();

		var result = await new CrossPostTargetStorage(context, new GuidFactory())
			.GetAccountAsync(account.Id, foreignUserId, Ct);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task CommentRepost_ExistsAsync_WithForeignUser_ShouldReturnFalseInDeleteAndUpdate()
	{
		var settings = await CreateCommentRepostSettingsAsync();

		var delete = await new DeleteCommentRepostStorage(context).ExistsAsync(settings.Id, foreignUserId, Ct);
		var update = await new UpdateCommentRepostStorage(context).ExistsAsync(settings.Id, foreignUserId, Ct);

		delete.ShouldBeFalse();
		update.ShouldBeFalse();
	}

	[Fact]
	public async Task CreateCommentRepost_TelegramSessionExistsAndActiveAsync_ShouldReturnTrueOnlyForOwner()
	{
		var session = new TelegramSessionBuilder(context).WithIsActive(true).Create();
		var sut = new CreateCommentRepostStorage(context, new GuidFactory());

		var own = await sut.TelegramSessionExistsAndActiveAsync(session.Id, session.UserId, Ct);
		var foreign = await sut.TelegramSessionExistsAndActiveAsync(session.Id, foreignUserId, Ct);

		own.ShouldBeTrue();
		foreign.ShouldBeFalse();
	}

	[Fact]
	public async Task DeleteMessages_WithForeignUser_ShouldNotDeleteMessage()
	{
		var message = new MessageBuilder(context).Create();

		await new DeleteMessagesStorage(context).DeleteMessagesAsync([message.Id], foreignUserId, Ct);

		(await context.Messages.AsNoTracking().AnyAsync(x => x.Id == message.Id, Ct)).ShouldBeTrue();
	}

	[Fact]
	public async Task DeleteOpenRouterSetting_ExistsAsync_ShouldReturnTrueOnlyForOwner()
	{
		var setting = new OpenRouterSettingBuilder(context).Create();
		var sut = new DeleteOpenRouterSettingStorage(context);

		var own = await sut.ExistsAsync(setting.Id, setting.UserId, Ct);
		var foreign = await sut.ExistsAsync(setting.Id, foreignUserId, Ct);

		own.ShouldBeTrue();
		foreign.ShouldBeFalse();
	}

	[Fact]
	public async Task Proxy_ExistsAndList_WithForeignUser_ShouldNotSeeProxy()
	{
		var proxy = await CreateProxyAsync();

		var delete = await new DeleteProxyStorage(context).ExistsAsync(foreignUserId, proxy.Id, Ct);
		var update = await new UpdateProxyStorage(context).ExistsAsync(foreignUserId, proxy.Id, Ct);
		var list = await new ListProxiesStorage(context).GetByUserIdAsync(foreignUserId, Ct);
		var belongs = await new ListProxiesStorage(context).BelongsToUserAsync(foreignUserId, proxy.Id, Ct);

		delete.ShouldBeFalse();
		update.ShouldBeFalse();
		belongs.ShouldBeFalse();
		list.ShouldNotContain(x => x.Id == proxy.Id);
	}

	[Fact]
	public async Task DeleteSchedule_ScheduleExistAsync_WithForeignUser_ShouldReturnFalse()
	{
		var schedule = new ScheduleBuilder(context).Create();

		var result = await new DeleteScheduleStorage(context).ScheduleExistAsync(schedule.Id, foreignUserId);

		result.ShouldBeFalse();
	}

	[Fact]
	public async Task DeleteTelegramBot_WithForeignUser_ShouldNotDeleteBot()
	{
		var bot = new TelegramBotBuilder(context).Create();

		await new DeleteTelegramBotStorage(context).DeleteTelegramBotAsync(bot.Id, foreignUserId, Ct);

		(await context.TelegramBots.AsNoTracking().AnyAsync(x => x.Id == bot.Id, Ct)).ShouldBeTrue();
	}

	[Fact]
	public async Task TelegramSession_ExistsChecks_WithForeignUser_ShouldReturnFalse()
	{
		var session = new TelegramSessionBuilder(context).Create();

		var delete = await new DeleteTelegramSessionStorage(context).ExistsAsync(foreignUserId, session.Id, Ct);
		var sendPassword = await new SendPasswordStorage(context).ExistsAsync(foreignUserId, session.Id, Ct);
		var startAuth = await new StartAuthStorage(context).ExistsAsync(foreignUserId, session.Id, Ct);
		var verifyCode = await new VerifyCodeStorage(context).ExistsAsync(foreignUserId, session.Id, Ct);
		var update = await new UpdateTelegramSessionStorage(context).GetByIdAsync(foreignUserId, session.Id, Ct);
		var list = await new ListTelegramSessionsStorage(context).GetByUserIdAsync(foreignUserId, Ct);

		delete.ShouldBeFalse();
		sendPassword.ShouldBeFalse();
		startAuth.ShouldBeFalse();
		verifyCode.ShouldBeFalse();
		update.ShouldBeNull();
		list.ShouldNotContain(x => x.Id == session.Id);
	}

	[Fact]
	public async Task ChannelCleanup_SessionExistsAsync_WithForeignUser_ShouldReturnFalse()
	{
		var session = new TelegramSessionBuilder(context).Create();

		var preview = await new PreviewChannelCleanupStorage(context)
			.SessionExistsAsync(session.Id, foreignUserId, Ct);
		var start = await new StartChannelCleanupStorage(context, new GuidFactory())
			.SessionExistsAsync(session.Id, foreignUserId, Ct);

		preview.ShouldBeFalse();
		start.ShouldBeFalse();
	}

	[Fact]
	public async Task GetChannelCleanupJob_WithForeignUser_ShouldReturnNull()
	{
		var session = new TelegramSessionBuilder(context).Create();
		var job = new ChannelCleanupJob { Id = Guid.NewGuid(), TelegramSessionId = session.Id };
		context.Add(job);
		await context.SaveChangesAsync(Ct);

		var result = await new GetChannelCleanupJobStorage(context).GetJobStateAsync(job.Id, foreignUserId, Ct);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task YouTubeAccount_WithForeignUser_ShouldNotBeListedOrDeleted()
	{
		var account = new YouTubeAccountBuilder(context).Create();

		await new DeleteYouTubeAccountStorage(context).DeleteYouTubeAccountAsync(account.Id, foreignUserId, Ct);
		var foreignList = await new GetYouTubeAccountsStorage(context).GetYouTubeAccounts(foreignUserId, Ct);
		var ownList = await new GetYouTubeAccountsStorage(context).GetYouTubeAccounts(account.UserId, Ct);

		(await context.YouTubeAccounts.AsNoTracking().AnyAsync(x => x.Id == account.Id, Ct)).ShouldBeTrue();
		foreignList.ShouldNotContain(x => x.Id == account.Id);
		ownList.ShouldContain(x => x.Id == account.Id);
	}

	[Fact]
	public async Task EditPromptSetting_ExistPromptAsync_WithForeignUser_ShouldReturnFalse()
	{
		var prompt = new PromptSettingBuilder(context).Create();

		var result = await new EditPromptSettingStorage(context).ExistPromptAsync(prompt.Id, foreignUserId, Ct);

		result.ShouldBeFalse();
	}

	[Fact]
	public async Task ScheduleExistChecks_WithForeignUser_ShouldReturnFalse()
	{
		var schedule = new ScheduleBuilder(context).Create();

		var getDays = await new GetDaysStorage(context).ScheduleExistAsync(schedule.Id, foreignUserId, Ct);
		var shuffle = await new ShuffleMessagesStorage(context).ExistAsync(schedule.Id, foreignUserId, Ct);
		var updateAllTime = await new UpdateAllTimeStorage(context).ExistAsync(schedule.Id, foreignUserId, Ct);
		var getSchedule = await new GetScheduleStorage(context).GetScheduleAsync(schedule.Id, foreignUserId, Ct);
		var list = await new ListScheduleStorage(context).GetListScheduleAsync(foreignUserId, Ct);

		getDays.ShouldBeFalse();
		shuffle.ShouldBeFalse();
		updateAllTime.ShouldBeFalse();
		getSchedule.ShouldBeNull();
		list.ShouldNotContain(x => x.Id == schedule.Id);
	}

	[Fact]
	public async Task GetTime_ExistScheduleAsync_ShouldReturnTrueOnlyForOwner()
	{
		var schedule = new ScheduleBuilder(context).Create();
		var sut = new GetTimeStorage(context);

		var own = await sut.ExistScheduleAsync(schedule.Id, schedule.UserId, Ct);
		var foreign = await sut.ExistScheduleAsync(schedule.Id, foreignUserId, Ct);

		own.ShouldBeTrue();
		foreign.ShouldBeFalse();
	}

	[Fact]
	public async Task GetRepostImportJob_WithForeignUser_ShouldReturnNull()
	{
		var settings = new RepostSettingsBuilder(context).Create();
		var job = new RepostImportJob { Id = Guid.NewGuid(), RepostSettingsId = settings.Id };
		context.Add(job);
		await context.SaveChangesAsync(Ct);

		var result = await new GetRepostImportJobStorage(context).GetJobStateAsync(job.Id, foreignUserId, Ct);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetTelegramBot_WithForeignUser_ShouldReturnNull()
	{
		var message = new MessageBuilder(context).Create();
		var schedule = await context.Schedules.AsNoTracking().FirstAsync(x => x.Id == message.ScheduleId, Ct);
		var sut = new GetTelegramBotStorage(context);

		var byId = await sut.GetApiTokenAsync(schedule.TelegramBotId, foreignUserId, Ct);
		var bySchedule = await sut.GetTelegramBotByScheduleIdAsync(schedule.Id, foreignUserId, Ct);
		var byMessage = await sut.GetTelegramBotByMessageIdAsync(message.Id, foreignUserId, Ct);

		byId.ShouldBeNull();
		bySchedule.ShouldBeNull();
		byMessage.ShouldBeNull();
	}

	[Fact]
	public async Task ListTelegramBot_WithForeignUser_ShouldNotSeeBot()
	{
		var bot = new TelegramBotBuilder(context).Create();
		var sut = new ListTelegramBotStorage(context);

		var list = await sut.GetTelegramBotListAsync(foreignUserId, Ct);
		var belongs = await sut.BelongsToUserAsync(foreignUserId, bot.Id, Ct);

		list.ShouldNotContain(x => x.Id == bot.Id);
		belongs.ShouldBeFalse();
	}

	[Fact]
	public async Task LoadFilesMessage_WithForeignUser_ShouldReturnNull()
	{
		var message = new MessageBuilder(context).Create();
		var sut = new LoadFilesMessageStorage(context);

		var api = await sut.GetTelegramApiAsync(message.Id, foreignUserId, Ct);
		var bot = await sut.GetTelegramBotAsync(message.Id, foreignUserId, Ct);

		api.ShouldBeNull();
		bot.ShouldBeNull();
	}

	[Fact]
	public async Task GenerateAiContent_WithForeignUser_ShouldReturnNull()
	{
		var message = new MessageBuilder(context).Create();
		var ownerId = await context.Schedules.Where(x => x.Id == message.ScheduleId).Select(x => x.UserId)
			.FirstAsync(Ct);
		var sut = new GenerateAiContentStorage(context);

		var own = await sut.GetMessageAsync(message.Id, ownerId, Ct);
		var foreign = await sut.GetMessageAsync(message.Id, foreignUserId, Ct);
		var foreignOpenRouter = await sut.GetOpenRouterAsync(message.Id, foreignUserId, Ct);

		own.ShouldNotBeNull();
		foreign.ShouldBeNull();
		foreignOpenRouter.ShouldBeNull();
	}

	[Fact]
	public async Task SendVideoOnYouTube_WithForeignUser_ShouldReturnNothing()
	{
		var message = new MessageBuilder(context).WithVideoMessageFile().Create();
		var sut = new SendVideoOnYouTubeStorage(context);

		var files = await sut.GetVideoFileMessageAsync(message.Id, foreignUserId, Ct);
		var account = await sut.GetAccessTokenAsync(message.Id, foreignUserId, Ct);

		files.ShouldBeEmpty();
		account.ShouldBeNull();
	}

	[Fact]
	public async Task ParseChannel_WithForeignUser_ShouldNotSeeScheduleAndSession()
	{
		var schedule = new ScheduleBuilder(context).Create();
		var session = new TelegramSessionBuilder(context).Create();
		var sut = new ParseChannelStorage(context, new GuidFactory());

		var token = await sut.GetTelegramTokenAsync(schedule.Id, foreignUserId, Ct);
		var ownSession = await sut.TelegramSessionExistsAsync(session.Id, session.UserId, Ct);
		var foreignSession = await sut.TelegramSessionExistsAsync(session.Id, foreignUserId, Ct);

		token.ShouldBeNull();
		ownSession.ShouldBeTrue();
		foreignSession.ShouldBeFalse();
	}

	[Fact]
	public async Task ListParseChannels_WithForeignUser_ShouldNotContainSource()
	{
		var source = new ChannelParsingSettingBuilder(context).Create();

		var result = await new ListParseChannelsStorage(context).GetChannelParsingParametersAsync(foreignUserId, Ct);

		result.ShouldNotContain(x => x.Id == source.Id);
	}

	[Fact]
	public async Task UpdateParseChannel_OwnerChecks_ShouldAcceptOnlyOwnerResources()
	{
		var schedule = new ScheduleBuilder(context).Create();
		var source = new ChannelParsingSettingBuilder(context).WithScheduleId(schedule.Id).Create();
		var session = new TelegramSessionBuilder(context).WithUserId(schedule.UserId).Create();
		var sut = new UpdateParseChannelStorage(context);

		(await sut.ExistParseChannelAsync(source.Id, foreignUserId, Ct)).ShouldBeFalse();
		(await sut.ExistScheduleAsync(schedule.Id, schedule.UserId, Ct)).ShouldBeTrue();
		(await sut.ExistScheduleAsync(schedule.Id, foreignUserId, Ct)).ShouldBeFalse();
		(await sut.ExistTelegramSessionAsync(session.Id, schedule.UserId, Ct)).ShouldBeTrue();
		(await sut.ExistTelegramSessionAsync(session.Id, foreignUserId, Ct)).ShouldBeFalse();
	}

	[Fact]
	public async Task RefreshParseChannelInfo_ShouldReturnInfoOnlyForOwner()
	{
		var schedule = new ScheduleBuilder(context).Create();
		var source = new ChannelParsingSettingBuilder(context).WithScheduleId(schedule.Id).Create();
		var sut = new RefreshParseChannelInfoStorage(context);

		var own = await sut.GetParseChannelInfoAsync(source.Id, schedule.UserId, Ct);
		var foreign = await sut.GetParseChannelInfoAsync(source.Id, foreignUserId, Ct);

		own.ShouldNotBeNull();
		foreign.ShouldBeNull();
	}

	[Fact]
	public async Task RepostLogs_WithForeignUser_ShouldReturnNothing()
	{
		var destination = new RepostDestinationBuilder(context).Create();
		var message = new MessageBuilder(context).Create();
		context.Add(new RepostLog
		{
			Id = Guid.NewGuid(),
			MessageId = message.Id,
			RepostDestinationId = destination.Id,
			Status = RepostStatus.Success
		});
		await context.SaveChangesAsync(Ct);
		var sut = new RepostLogsStorage(context);

		var logs = await sut.GetRepostLogsAsync(foreignUserId,
			new ListRepostLogsQuery(destination.RepostSettingsId, null, null, null, null, null, 1, 50), Ct);
		var summary = await sut.GetSummaryAsync(foreignUserId,
			new GetRepostLogsSummaryQuery(destination.RepostSettingsId, null, null, null, null), Ct);

		logs.TotalCount.ShouldBe(0);
		summary.Total.ShouldBe(0);
	}

	[Fact]
	public async Task SocialAccount_ExistsAsync_WithForeignUser_ShouldReturnFalse()
	{
		var account = new SocialAccountBuilder(context).Create();

		var result = await new SocialAccountStorage(context, new GuidFactory())
			.ExistsAsync(account.Id, foreignUserId, Ct);

		result.ShouldBeFalse();
	}

	[Fact]
	public async Task UpdateSchedule_WithForeignUser_ShouldNotReadOrChangeSchedule()
	{
		var schedule = new ScheduleBuilder(context).Create();
		var sut = new UpdateScheduleStorage(context);

		var token = await sut.GetApiTokenAsync(schedule.TelegramBotId, foreignUserId, Ct);
		var channel = await sut.GetChannelNameAsync(schedule.Id, foreignUserId, Ct);
		await sut.UpdateScheduleAsync(schedule.Id, foreignUserId, "Чужое имя", null, null, null, null, Ct);

		token.ShouldBeNull();
		channel.ShouldBeNull();
		(await context.Schedules.AsNoTracking().FirstAsync(x => x.Id == schedule.Id, Ct)).Name
			.ShouldBe(schedule.Name);
	}

	[Fact]
	public async Task UpdateSchedule_YouTubeAccountExistsAsync_ShouldReturnTrueOnlyForOwner()
	{
		var account = new YouTubeAccountBuilder(context).Create();
		var sut = new UpdateScheduleStorage(context);

		var own = await sut.YouTubeAccountExistsAsync(account.Id, account.UserId, Ct);
		var foreign = await sut.YouTubeAccountExistsAsync(account.Id, foreignUserId, Ct);

		own.ShouldBeTrue();
		foreign.ShouldBeFalse();
	}

	private async Task<CommentRepostSettings> CreateCommentRepostSettingsAsync()
	{
		var schedule = new ScheduleBuilder(context).Create();
		var session = new TelegramSessionBuilder(context).WithUserId(schedule.UserId).Create();
		var settings = new CommentRepostSettings
		{
			Id = Guid.NewGuid(),
			WatchedChannel = "watched",
			WatchedChannelId = Random.Shared.NextInt64(1, long.MaxValue),
			DiscussionGroupId = Random.Shared.NextInt64(1, long.MaxValue),
			TelegramSessionId = session.Id,
			ScheduleId = schedule.Id
		};
		context.Add(settings);
		await context.SaveChangesAsync(Ct);
		return settings;
	}

	private async Task<Proxy> CreateProxyAsync()
	{
		var user = new UserBuilder(context).Create();
		var proxy = new Proxy
		{
			Id = Guid.NewGuid(),
			Name = "proxy",
			Type = Data.Enum.ProxyType.Socks5,
			Host = "127.0.0.1",
			Port = 1080,
			UserId = user.Id
		};
		context.Add(proxy);
		await context.SaveChangesAsync(Ct);
		return proxy;
	}
}
