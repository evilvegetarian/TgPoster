using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Entities;

namespace TgPoster.Storage.Data;

public class PosterDbContext(DbContextOptions<PosterDbContext> options) : DbContext(options)
{

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}