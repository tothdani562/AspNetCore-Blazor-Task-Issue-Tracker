using Microsoft.EntityFrameworkCore;
using TaskTracker.Web.Models;

namespace TaskTracker.Web.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User entity konfigurálása
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.HasIndex(e => e.Email)
                .IsUnique();
            
            entity.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(e => e.PasswordHash)
                .IsRequired();

            entity.Property(e => e.RefreshTokenHash)
                .HasMaxLength(256);

            entity.Property(e => e.RefreshTokenExpiresAt);
            
            entity.Property(e => e.CreatedAt);
            
            entity.Property(e => e.UpdatedAt);
        });
    }
}

