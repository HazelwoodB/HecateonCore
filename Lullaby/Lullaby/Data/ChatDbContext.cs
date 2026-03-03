using Lullaby.Models;
using Microsoft.EntityFrameworkCore;

namespace Lullaby.Data;

public class ChatDbContext(DbContextOptions<ChatDbContext> options) : DbContext(options)
{
    public DbSet<ChatMessage> ChatMessages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure ChatMessage entity
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Role)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Message)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.Timestamp)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.Sentiment)
                .HasMaxLength(50);

            entity.Property(e => e.Score);

            // Create index for faster queries
            entity.HasIndex(e => e.Timestamp)
                .IsDescending()
                .HasDatabaseName("IX_ChatMessages_Timestamp_Desc");

            entity.HasIndex(e => e.Role)
                .HasDatabaseName("IX_ChatMessages_Role");
        });
    }
}
