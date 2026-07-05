using Microsoft.EntityFrameworkCore;
using TmsApi.Entities;
namespace TmsApi.Data;
public class TmsDbContext(DbContextOptions<TmsDbContext> options) : DbContext(options)
{
public DbSet<Student> Students => Set<Student>();
public DbSet<Course> Courses => Set<Course>();
public DbSet<Enrollment> Enrollments => Set<Enrollment>();
public DbSet<Assessment> Assessments => Set<Assessment>();
public DbSet<Certificate> Certificates => Set<Certificate>();

public override int SaveChanges()
{
    UpdateAuditStamps();
    return base.SaveChanges();
}

public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    UpdateAuditStamps();
    return base.SaveChangesAsync(cancellationToken);
}

private void UpdateAuditStamps()
{
    var entries = ChangeTracker.Entries()
        .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

    foreach (var entry in entries)
    {
        // Dynamically locate and write values into the hidden database shadow field
        if (entry.Metadata.FindProperty("LastUpdated") != null)
        {
            entry.Property("LastUpdated").CurrentValue = DateTime.UtcNow;
        }
    }
}
}

// protected override void OnModelCreating(ModelBuilder modelBuilder)
// {
//     base.OnModelCreating(modelBuilder);
    
//     // Scans this project's assembly for all classes implementing IEntityTypeConfiguration
//     modelBuilder.ApplyConfigurationsFromAssembly(typeof(TmsDbContext).Assembly); 
// }

