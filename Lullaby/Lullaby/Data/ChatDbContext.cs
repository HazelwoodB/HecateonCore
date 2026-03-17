using Hecateon.Models;
using Hecateon.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Hecateon.Data;

public class ChatDbContext(DbContextOptions<ChatDbContext> options) : DbContext(options)
{
    public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
    public DbSet<StreamEventRecord> StreamEvents { get; set; } = null!;
    public DbSet<GraphNodeRecord> GraphNodes { get; set; } = null!;
    public DbSet<GraphEdgeRecord> GraphEdges { get; set; } = null!;
    public DbSet<GraphEvidenceRecord> GraphEvidence { get; set; } = null!;
    public DbSet<GraphProjectionStateRecord> GraphProjectionStates { get; set; } = null!;
    public DbSet<ModeStateRecord> ModeStates { get; set; } = null!;
    public DbSet<NyphosPreferenceRecord> NyphosPreferences { get; set; } = null!;

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
                .IsRequired();

            var timestampProperty = entity.Property(e => e.Timestamp)
                .IsRequired();

            if (Database.IsSqlServer())
            {
                entity.Property(e => e.Message)
                    .HasColumnType("nvarchar(max)");

                timestampProperty.HasDefaultValueSql("GETUTCDATE()");
            }
            else if (Database.IsSqlite())
            {
                timestampProperty.HasDefaultValueSql("CURRENT_TIMESTAMP");
            }

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

        modelBuilder.Entity<StreamEventRecord>(entity =>
        {
            entity.ToTable("stream_events");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.EventId)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(128);

            entity.Property(e => e.DeviceId)
                .IsRequired()
                .HasMaxLength(128);

            entity.Property(e => e.Stream)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(e => e.Type)
                .IsRequired()
                .HasMaxLength(128);

            entity.Property(e => e.Seq)
                .IsRequired();

            entity.Property(e => e.TimestampUtc)
                .IsRequired();

            entity.Property(e => e.SchemaVersion)
                .IsRequired();

            entity.Property(e => e.PayloadJson)
                .IsRequired();

            entity.Property(e => e.ClientMsgId)
                .IsRequired()
                .HasMaxLength(128);

            entity.HasIndex(e => new { e.UserId, e.DeviceId, e.ClientMsgId })
                .IsUnique()
                .HasDatabaseName("UX_StreamEvents_Idempotency");

            entity.HasIndex(e => new { e.Stream, e.Seq })
                .IsUnique()
                .HasDatabaseName("UX_StreamEvents_StreamSeq");

            entity.HasIndex(e => e.Stream)
                .HasDatabaseName("IX_StreamEvents_Stream");
        });

        modelBuilder.Entity<GraphNodeRecord>(entity =>
        {
            entity.ToTable("graph_nodes");
            entity.HasKey(e => e.NodeId);

            entity.Property(e => e.NodeId).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(64);
            entity.Property(e => e.CanonicalLabel).IsRequired().HasMaxLength(256);
            entity.Property(e => e.AliasesJson).IsRequired();
            entity.Property(e => e.CreatedUtc).IsRequired();
            entity.Property(e => e.UpdatedUtc).IsRequired();
            entity.Property(e => e.Salience).IsRequired();

            entity.HasIndex(e => e.Type).HasDatabaseName("IX_GraphNodes_Type");
            entity.HasIndex(e => e.UpdatedUtc).HasDatabaseName("IX_GraphNodes_UpdatedUtc");
        });

        modelBuilder.Entity<GraphEdgeRecord>(entity =>
        {
            entity.ToTable("graph_edges");
            entity.HasKey(e => e.EdgeId);

            entity.Property(e => e.EdgeId).IsRequired().HasMaxLength(128);
            entity.Property(e => e.FromId).IsRequired().HasMaxLength(128);
            entity.Property(e => e.ToId).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(64);
            entity.Property(e => e.Weight).IsRequired();
            entity.Property(e => e.CreatedUtc).IsRequired();
            entity.Property(e => e.UpdatedUtc).IsRequired();

            entity.HasIndex(e => new { e.FromId, e.ToId, e.Type }).HasDatabaseName("IX_GraphEdges_FromToType");
            entity.HasIndex(e => e.UpdatedUtc).HasDatabaseName("IX_GraphEdges_UpdatedUtc");
        });

        modelBuilder.Entity<GraphEvidenceRecord>(entity =>
        {
            entity.ToTable("graph_evidence");
            entity.HasKey(e => e.EvidenceId);

            entity.Property(e => e.EvidenceId).IsRequired().HasMaxLength(128);
            entity.Property(e => e.NodeId).HasMaxLength(128);
            entity.Property(e => e.EdgeId).HasMaxLength(128);
            entity.Property(e => e.SourceEventId).IsRequired().HasMaxLength(64);
            entity.Property(e => e.Snippet).IsRequired();
            entity.Property(e => e.Confidence).IsRequired();
            entity.Property(e => e.CreatedUtc).IsRequired();

            entity.HasIndex(e => e.NodeId).HasDatabaseName("IX_GraphEvidence_NodeId");
            entity.HasIndex(e => e.EdgeId).HasDatabaseName("IX_GraphEvidence_EdgeId");
            entity.HasIndex(e => e.SourceEventId).HasDatabaseName("IX_GraphEvidence_SourceEventId");
        });

        modelBuilder.Entity<GraphProjectionStateRecord>(entity =>
        {
            entity.ToTable("graph_projection_state");
            entity.HasKey(e => e.ProjectionName);

            entity.Property(e => e.ProjectionName).IsRequired().HasMaxLength(64);
            entity.Property(e => e.LastAppliedSeq).IsRequired();
            entity.Property(e => e.UpdatedUtc).IsRequired();
        });

        modelBuilder.Entity<ModeStateRecord>(entity =>
        {
            entity.ToTable("mode_state");
            entity.HasKey(e => new { e.UserId, e.DeviceId });

            entity.Property(e => e.UserId).IsRequired().HasMaxLength(128);
            entity.Property(e => e.DeviceId).IsRequired().HasMaxLength(128);
            entity.Property(e => e.CurrentMode).IsRequired().HasMaxLength(32);
            entity.Property(e => e.PreviousMode).HasMaxLength(32);
            entity.Property(e => e.LastConfidence).IsRequired();
            entity.Property(e => e.LastSource).IsRequired().HasMaxLength(32);
            entity.Property(e => e.LastEvidenceEventIdsJson).IsRequired();
            entity.Property(e => e.UpdatedUtc).IsRequired();

            entity.HasIndex(e => e.UpdatedUtc).HasDatabaseName("IX_ModeState_UpdatedUtc");
        });

        modelBuilder.Entity<NyphosPreferenceRecord>(entity =>
        {
            entity.ToTable("nyphos_preferences");
            entity.HasKey(e => new { e.UserId, e.DeviceId });

            entity.Property(e => e.UserId).IsRequired().HasMaxLength(128);
            entity.Property(e => e.DeviceId).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Tone).IsRequired().HasMaxLength(16);
            entity.Property(e => e.MutedUntilUtc).IsRequired(false);
            entity.Property(e => e.UpdatedUtc).IsRequired();

            entity.HasIndex(e => e.UpdatedUtc).HasDatabaseName("IX_NyphosPreferences_UpdatedUtc");
        });
    }
}
