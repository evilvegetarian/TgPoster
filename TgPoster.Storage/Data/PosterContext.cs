using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Security.IdentityServices;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data;

internal class PosterContext(
	DbContextOptions<PosterContext> options,
	IIdentityProvider identityProvider) : DbContext(options)
{
	public DbSet<User> Users { get; set; }
	public DbSet<RefreshSession> RefreshSessions { get; set; }
	public DbSet<Day> Days { get; set; }
	public DbSet<Schedule> Schedules { get; set; }
	public DbSet<TelegramBot> TelegramBots { get; set; }
	public DbSet<Message> Messages { get; set; }
	public DbSet<MessageFile> MessageFiles { get; set; }
	public DbSet<ChannelParsingSetting> ChannelParsingParameters { get; set; }
	public DbSet<OpenRouterSetting> OpenRouterSettings { get; set; }
	public DbSet<PromptSetting> PromptSettings { get; set; }
	public DbSet<YouTubeAccount> YouTubeAccounts { get; set; }
	public DbSet<TelegramSession> TelegramSessions { get; set; }
	public DbSet<RepostDestination> RepostDestinations { get; set; }
	public DbSet<RepostSettings> RepostSettings { get; set; }
	public DbSet<CommentRepostSettings> CommentRepostSettings { get; set; }
	public DbSet<CommentRepostLog> CommentRepostLogs { get; set; }

	public override Task<int> SaveChangesAsync(CancellationToken ct = new())
	{
		var entries = ChangeTracker.Entries<BaseEntity>()
			.Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);

		foreach (var entityEntry in entries)
		{
			if (entityEntry.State == EntityState.Added)
			{
				entityEntry.Entity.Created = DateTimeOffset.UtcNow;
				entityEntry.Entity.CreatedById = identityProvider.Current.IsAuthenticated
					? identityProvider.Current.UserId
					: null;
			}
			else if (entityEntry.State == EntityState.Modified)
			{
				entityEntry.Property(x => x.Created).IsModified = false;
				entityEntry.Property(x => x.CreatedById).IsModified = false;
				entityEntry.Entity.Updated = DateTimeOffset.UtcNow;
				entityEntry.Entity.UpdatedById = identityProvider.Current.IsAuthenticated
					? identityProvider.Current.UserId
					: null;
			}
			else if (entityEntry.State == EntityState.Deleted)
			{
				entityEntry.State = EntityState.Modified;
				entityEntry.Property(x => x.Created).IsModified = false;
				entityEntry.Property(x => x.CreatedById).IsModified = false;
				entityEntry.Property(x => x.Updated).IsModified = false;
				entityEntry.Property(x => x.UpdatedById).IsModified = false;
				entityEntry.Entity.Deleted = DateTimeOffset.UtcNow;
				entityEntry.Entity.DeletedById = identityProvider.Current.IsAuthenticated
					? identityProvider.Current.UserId
					: null;
			}
		}

		return base.SaveChangesAsync(ct);
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
	}
}