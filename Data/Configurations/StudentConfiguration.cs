using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Entities;

namespace TmsApi.Data.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.RegistrationNumber)
            .IsRequired()
            .HasMaxLength(20);

        // Session 2 Natural Key Uniqueness Constraint
        builder.HasIndex(s => s.RegistrationNumber)
            .IsUnique();

        builder.Property<DateTime>("LastUpdated");

        builder.Property(s => s.Version)
            .IsRowVersion(); // Automatically hooks into PostgreSQL's hidden 'xmin' system column
    
        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}