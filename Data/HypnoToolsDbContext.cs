using HypnoTools.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HypnoTools.API.Data;

public class HypnoToolsDbContext : DbContext
{
    public HypnoToolsDbContext(DbContextOptions<HypnoToolsDbContext> options) : base(options)
    {
    }

    public DbSet<Client> Clients { get; set; }
    public DbSet<Implementation> Implementations { get; set; }
    public DbSet<ImplementationTask> ImplementationTasks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global query filters for soft delete
        modelBuilder.Entity<Client>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Implementation>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<ImplementationTask>().HasQueryFilter(e => e.DeletedAt == null);

        // Configure relationships
        modelBuilder.Entity<Implementation>()
            .HasOne(i => i.Client)
            .WithMany(c => c.Implementations)
            .HasForeignKey(i => i.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ImplementationTask>()
            .HasOne(t => t.Implementation)
            .WithMany(i => i.Tasks)
            .HasForeignKey(t => t.ImplementationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes
        modelBuilder.Entity<Client>()
            .HasIndex(c => c.Email)
            .IsUnique();

        modelBuilder.Entity<Client>()
            .HasIndex(c => c.Company);

        modelBuilder.Entity<Implementation>()
            .HasIndex(i => i.Status);

        modelBuilder.Entity<ImplementationTask>()
            .HasIndex(t => new { t.ImplementationId, t.SortOrder });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}