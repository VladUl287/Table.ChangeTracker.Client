using Microsoft.EntityFrameworkCore;
using Tracker.Api.Demo.Database.Entities;

namespace Tracker.Api.Demo.Database;

public sealed class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder
            .Entity<Role>(builder =>
            {
                builder.HasKey(c => c.Id);
            });
    }
}