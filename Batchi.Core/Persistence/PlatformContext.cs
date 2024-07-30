using Batchi.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Batchi.Core.Persistence;

public class PlatformContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Job> Jobs { get; set; }
    public DbSet<JobDetails> JobDetails { get; set; }
    
    // I don't belong here, this is just for the Example project
    public DbSet<TestBooks> TestBooks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<Job>(entity =>
        {
            entity.Property(_ => _.Id).ValueGeneratedOnAdd();
            entity.HasOne(_ => _.JobDetails);
        });
        
        modelBuilder.Entity<JobDetails>(entity =>
        {
            entity.Property(_ => _.Id).ValueGeneratedOnAdd();
            entity.Property(_ => _.LastModifiedOn)
                .ValueGeneratedOnAddOrUpdate();
        });
        
        modelBuilder.Entity<Job>(entity =>
        {
            entity.Property(_ => _.Id).ValueGeneratedOnAdd();
        });
    }
}