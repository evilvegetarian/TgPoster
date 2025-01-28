using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Data;

public class PosterContext(DbContextOptions<PosterContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshSession> RefreshSessions { get; set; }
    public DbSet<Day> Days { get; set; }
    public DbSet<Schedule> Schedules { get; set; }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);

        foreach (var entityEntry in entries)
        {
            //TODO: Когда сделаю определение пользователя добавить пользователя сюда
            if (entityEntry.State == EntityState.Added)
            {
                entityEntry.Entity.Created = DateTime.UtcNow;
            }
            else if (entityEntry.State == EntityState.Modified)
            {
                entityEntry.Property(x => x.Created).IsModified = false;
                entityEntry.Property(x => x.CreatedById).IsModified = false;
                entityEntry.Entity.Updated = DateTime.UtcNow;
            }
            else if (entityEntry.State == EntityState.Deleted)
            {
                entityEntry.State = EntityState.Modified;
                entityEntry.Property(x => x.Created).IsModified = false;
                entityEntry.Property(x => x.CreatedById).IsModified = false;
                entityEntry.Property(x => x.Updated).IsModified = false;
                entityEntry.Property(x => x.UpdatedById).IsModified = false;
                entityEntry.Entity.Deleted = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}