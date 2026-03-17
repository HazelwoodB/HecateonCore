using Hecateon.Data;
using Microsoft.EntityFrameworkCore;

namespace Hecateon.Data;

public static class DatabaseExtensions
{
    /// <summary>
    /// Applies pending migrations and initializes the database.
    /// Should be called during application startup.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();

        try
        {
            if (dbContext.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true)
            {
                await dbContext.Database.MigrateAsync();
            }
            else
            {
                await dbContext.Database.EnsureCreatedAsync();
            }

            await EnsureStreamEventSchemaAsync(dbContext);
            await EnsureGraphSchemaAsync(dbContext);
            await EnsureModeSchemaAsync(dbContext);
            await EnsureNyphosPreferenceSchemaAsync(dbContext);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database initialization error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Synchronous version for use in Program.cs if async is not available.
    /// </summary>
    public static void InitializeDatabase(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();

        try
        {
            if (dbContext.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true)
            {
                dbContext.Database.Migrate();
            }
            else
            {
                dbContext.Database.EnsureCreated();
            }

            EnsureStreamEventSchema(dbContext);
            EnsureGraphSchema(dbContext);
            EnsureModeSchema(dbContext);
            EnsureNyphosPreferenceSchema(dbContext);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database initialization error: {ex.Message}");
            throw;
        }
    }

    private static async Task EnsureStreamEventSchemaAsync(ChatDbContext dbContext)
    {
        if (dbContext.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true)
        {
            await dbContext.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID('dbo.stream_events', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[stream_events](
        [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [EventId] NVARCHAR(64) NOT NULL,
        [UserId] NVARCHAR(128) NOT NULL,
        [DeviceId] NVARCHAR(128) NOT NULL,
        [Stream] NVARCHAR(64) NOT NULL,
        [Type] NVARCHAR(128) NOT NULL,
        [Seq] BIGINT NOT NULL,
        [TimestampUtc] DATETIMEOFFSET NOT NULL,
        [SchemaVersion] INT NOT NULL,
        [PayloadJson] NVARCHAR(MAX) NOT NULL,
        [ClientMsgId] NVARCHAR(128) NOT NULL
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_StreamEvents_Idempotency' AND object_id = OBJECT_ID('dbo.stream_events'))
BEGIN
    CREATE UNIQUE INDEX [UX_StreamEvents_Idempotency] ON [dbo].[stream_events]([UserId], [DeviceId], [ClientMsgId]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_StreamEvents_StreamSeq' AND object_id = OBJECT_ID('dbo.stream_events'))
BEGIN
    CREATE UNIQUE INDEX [UX_StreamEvents_StreamSeq] ON [dbo].[stream_events]([Stream], [Seq]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StreamEvents_Stream' AND object_id = OBJECT_ID('dbo.stream_events'))
BEGIN
    CREATE INDEX [IX_StreamEvents_Stream] ON [dbo].[stream_events]([Stream]);
END
");

            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS stream_events (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    EventId TEXT NOT NULL,
    UserId TEXT NOT NULL,
    DeviceId TEXT NOT NULL,
    Stream TEXT NOT NULL,
    Type TEXT NOT NULL,
    Seq INTEGER NOT NULL,
    TimestampUtc TEXT NOT NULL,
    SchemaVersion INTEGER NOT NULL,
    PayloadJson TEXT NOT NULL,
    ClientMsgId TEXT NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS UX_StreamEvents_Idempotency ON stream_events(UserId, DeviceId, ClientMsgId);
CREATE UNIQUE INDEX IF NOT EXISTS UX_StreamEvents_StreamSeq ON stream_events(Stream, Seq);
CREATE INDEX IF NOT EXISTS IX_StreamEvents_Stream ON stream_events(Stream);
");
    }

    private static void EnsureStreamEventSchema(ChatDbContext dbContext)
    {
        if (dbContext.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true)
        {
            dbContext.Database.ExecuteSqlRaw(@"
IF OBJECT_ID('dbo.stream_events', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[stream_events](
        [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [EventId] NVARCHAR(64) NOT NULL,
        [UserId] NVARCHAR(128) NOT NULL,
        [DeviceId] NVARCHAR(128) NOT NULL,
        [Stream] NVARCHAR(64) NOT NULL,
        [Type] NVARCHAR(128) NOT NULL,
        [Seq] BIGINT NOT NULL,
        [TimestampUtc] DATETIMEOFFSET NOT NULL,
        [SchemaVersion] INT NOT NULL,
        [PayloadJson] NVARCHAR(MAX) NOT NULL,
        [ClientMsgId] NVARCHAR(128) NOT NULL
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_StreamEvents_Idempotency' AND object_id = OBJECT_ID('dbo.stream_events'))
BEGIN
    CREATE UNIQUE INDEX [UX_StreamEvents_Idempotency] ON [dbo].[stream_events]([UserId], [DeviceId], [ClientMsgId]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_StreamEvents_StreamSeq' AND object_id = OBJECT_ID('dbo.stream_events'))
BEGIN
    CREATE UNIQUE INDEX [UX_StreamEvents_StreamSeq] ON [dbo].[stream_events]([Stream], [Seq]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StreamEvents_Stream' AND object_id = OBJECT_ID('dbo.stream_events'))
BEGIN
    CREATE INDEX [IX_StreamEvents_Stream] ON [dbo].[stream_events]([Stream]);
END
");

            return;
        }

        dbContext.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS stream_events (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    EventId TEXT NOT NULL,
    UserId TEXT NOT NULL,
    DeviceId TEXT NOT NULL,
    Stream TEXT NOT NULL,
    Type TEXT NOT NULL,
    Seq INTEGER NOT NULL,
    TimestampUtc TEXT NOT NULL,
    SchemaVersion INTEGER NOT NULL,
    PayloadJson TEXT NOT NULL,
    ClientMsgId TEXT NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS UX_StreamEvents_Idempotency ON stream_events(UserId, DeviceId, ClientMsgId);
CREATE UNIQUE INDEX IF NOT EXISTS UX_StreamEvents_StreamSeq ON stream_events(Stream, Seq);
CREATE INDEX IF NOT EXISTS IX_StreamEvents_Stream ON stream_events(Stream);
");
    }

    private static async Task EnsureGraphSchemaAsync(ChatDbContext dbContext)
    {
        if (dbContext.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true)
        {
            await dbContext.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID('dbo.graph_nodes', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[graph_nodes](
        [NodeId] NVARCHAR(128) NOT NULL PRIMARY KEY,
        [Type] NVARCHAR(64) NOT NULL,
        [CanonicalLabel] NVARCHAR(256) NOT NULL,
        [AliasesJson] NVARCHAR(MAX) NOT NULL,
        [CreatedUtc] DATETIMEOFFSET NOT NULL,
        [UpdatedUtc] DATETIMEOFFSET NOT NULL,
        [Salience] FLOAT NOT NULL
    );
END

IF OBJECT_ID('dbo.graph_edges', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[graph_edges](
        [EdgeId] NVARCHAR(128) NOT NULL PRIMARY KEY,
        [FromId] NVARCHAR(128) NOT NULL,
        [ToId] NVARCHAR(128) NOT NULL,
        [Type] NVARCHAR(64) NOT NULL,
        [Weight] FLOAT NOT NULL,
        [CreatedUtc] DATETIMEOFFSET NOT NULL,
        [UpdatedUtc] DATETIMEOFFSET NOT NULL
    );
END

IF OBJECT_ID('dbo.graph_evidence', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[graph_evidence](
        [EvidenceId] NVARCHAR(128) NOT NULL PRIMARY KEY,
        [NodeId] NVARCHAR(128) NULL,
        [EdgeId] NVARCHAR(128) NULL,
        [SourceEventId] NVARCHAR(64) NOT NULL,
        [Snippet] NVARCHAR(MAX) NOT NULL,
        [Confidence] FLOAT NOT NULL,
        [CreatedUtc] DATETIMEOFFSET NOT NULL
    );
END

IF OBJECT_ID('dbo.graph_projection_state', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[graph_projection_state](
        [ProjectionName] NVARCHAR(64) NOT NULL PRIMARY KEY,
        [LastAppliedSeq] BIGINT NOT NULL,
        [UpdatedUtc] DATETIMEOFFSET NOT NULL
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GraphNodes_Type' AND object_id = OBJECT_ID('dbo.graph_nodes'))
BEGIN
    CREATE INDEX [IX_GraphNodes_Type] ON [dbo].[graph_nodes]([Type]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GraphNodes_UpdatedUtc' AND object_id = OBJECT_ID('dbo.graph_nodes'))
BEGIN
    CREATE INDEX [IX_GraphNodes_UpdatedUtc] ON [dbo].[graph_nodes]([UpdatedUtc]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GraphEdges_FromToType' AND object_id = OBJECT_ID('dbo.graph_edges'))
BEGIN
    CREATE INDEX [IX_GraphEdges_FromToType] ON [dbo].[graph_edges]([FromId], [ToId], [Type]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GraphEdges_UpdatedUtc' AND object_id = OBJECT_ID('dbo.graph_edges'))
BEGIN
    CREATE INDEX [IX_GraphEdges_UpdatedUtc] ON [dbo].[graph_edges]([UpdatedUtc]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GraphEvidence_NodeId' AND object_id = OBJECT_ID('dbo.graph_evidence'))
BEGIN
    CREATE INDEX [IX_GraphEvidence_NodeId] ON [dbo].[graph_evidence]([NodeId]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GraphEvidence_EdgeId' AND object_id = OBJECT_ID('dbo.graph_evidence'))
BEGIN
    CREATE INDEX [IX_GraphEvidence_EdgeId] ON [dbo].[graph_evidence]([EdgeId]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GraphEvidence_SourceEventId' AND object_id = OBJECT_ID('dbo.graph_evidence'))
BEGIN
    CREATE INDEX [IX_GraphEvidence_SourceEventId] ON [dbo].[graph_evidence]([SourceEventId]);
END
");

            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS graph_nodes (
    NodeId TEXT NOT NULL PRIMARY KEY,
    Type TEXT NOT NULL,
    CanonicalLabel TEXT NOT NULL,
    AliasesJson TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    UpdatedUtc TEXT NOT NULL,
    Salience REAL NOT NULL
);

CREATE TABLE IF NOT EXISTS graph_edges (
    EdgeId TEXT NOT NULL PRIMARY KEY,
    FromId TEXT NOT NULL,
    ToId TEXT NOT NULL,
    Type TEXT NOT NULL,
    Weight REAL NOT NULL,
    CreatedUtc TEXT NOT NULL,
    UpdatedUtc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS graph_evidence (
    EvidenceId TEXT NOT NULL PRIMARY KEY,
    NodeId TEXT NULL,
    EdgeId TEXT NULL,
    SourceEventId TEXT NOT NULL,
    Snippet TEXT NOT NULL,
    Confidence REAL NOT NULL,
    CreatedUtc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS graph_projection_state (
    ProjectionName TEXT NOT NULL PRIMARY KEY,
    LastAppliedSeq INTEGER NOT NULL,
    UpdatedUtc TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_GraphNodes_Type ON graph_nodes(Type);
CREATE INDEX IF NOT EXISTS IX_GraphNodes_UpdatedUtc ON graph_nodes(UpdatedUtc);
CREATE INDEX IF NOT EXISTS IX_GraphEdges_FromToType ON graph_edges(FromId, ToId, Type);
CREATE INDEX IF NOT EXISTS IX_GraphEdges_UpdatedUtc ON graph_edges(UpdatedUtc);
CREATE INDEX IF NOT EXISTS IX_GraphEvidence_NodeId ON graph_evidence(NodeId);
CREATE INDEX IF NOT EXISTS IX_GraphEvidence_EdgeId ON graph_evidence(EdgeId);
CREATE INDEX IF NOT EXISTS IX_GraphEvidence_SourceEventId ON graph_evidence(SourceEventId);
");
    }

    private static void EnsureGraphSchema(ChatDbContext dbContext)
    {
        if (dbContext.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true)
        {
            dbContext.Database.ExecuteSqlRaw(@"
IF OBJECT_ID('dbo.graph_nodes', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[graph_nodes](
        [NodeId] NVARCHAR(128) NOT NULL PRIMARY KEY,
        [Type] NVARCHAR(64) NOT NULL,
        [CanonicalLabel] NVARCHAR(256) NOT NULL,
        [AliasesJson] NVARCHAR(MAX) NOT NULL,
        [CreatedUtc] DATETIMEOFFSET NOT NULL,
        [UpdatedUtc] DATETIMEOFFSET NOT NULL,
        [Salience] FLOAT NOT NULL
    );
END

IF OBJECT_ID('dbo.graph_edges', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[graph_edges](
        [EdgeId] NVARCHAR(128) NOT NULL PRIMARY KEY,
        [FromId] NVARCHAR(128) NOT NULL,
        [ToId] NVARCHAR(128) NOT NULL,
        [Type] NVARCHAR(64) NOT NULL,
        [Weight] FLOAT NOT NULL,
        [CreatedUtc] DATETIMEOFFSET NOT NULL,
        [UpdatedUtc] DATETIMEOFFSET NOT NULL
    );
END

IF OBJECT_ID('dbo.graph_evidence', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[graph_evidence](
        [EvidenceId] NVARCHAR(128) NOT NULL PRIMARY KEY,
        [NodeId] NVARCHAR(128) NULL,
        [EdgeId] NVARCHAR(128) NULL,
        [SourceEventId] NVARCHAR(64) NOT NULL,
        [Snippet] NVARCHAR(MAX) NOT NULL,
        [Confidence] FLOAT NOT NULL,
        [CreatedUtc] DATETIMEOFFSET NOT NULL
    );
END

IF OBJECT_ID('dbo.graph_projection_state', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[graph_projection_state](
        [ProjectionName] NVARCHAR(64) NOT NULL PRIMARY KEY,
        [LastAppliedSeq] BIGINT NOT NULL,
        [UpdatedUtc] DATETIMEOFFSET NOT NULL
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GraphNodes_Type' AND object_id = OBJECT_ID('dbo.graph_nodes'))
BEGIN
    CREATE INDEX [IX_GraphNodes_Type] ON [dbo].[graph_nodes]([Type]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GraphNodes_UpdatedUtc' AND object_id = OBJECT_ID('dbo.graph_nodes'))
BEGIN
    CREATE INDEX [IX_GraphNodes_UpdatedUtc] ON [dbo].[graph_nodes]([UpdatedUtc]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GraphEdges_FromToType' AND object_id = OBJECT_ID('dbo.graph_edges'))
BEGIN
    CREATE INDEX [IX_GraphEdges_FromToType] ON [dbo].[graph_edges]([FromId], [ToId], [Type]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GraphEdges_UpdatedUtc' AND object_id = OBJECT_ID('dbo.graph_edges'))
BEGIN
    CREATE INDEX [IX_GraphEdges_UpdatedUtc] ON [dbo].[graph_edges]([UpdatedUtc]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GraphEvidence_NodeId' AND object_id = OBJECT_ID('dbo.graph_evidence'))
BEGIN
    CREATE INDEX [IX_GraphEvidence_NodeId] ON [dbo].[graph_evidence]([NodeId]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GraphEvidence_EdgeId' AND object_id = OBJECT_ID('dbo.graph_evidence'))
BEGIN
    CREATE INDEX [IX_GraphEvidence_EdgeId] ON [dbo].[graph_evidence]([EdgeId]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GraphEvidence_SourceEventId' AND object_id = OBJECT_ID('dbo.graph_evidence'))
BEGIN
    CREATE INDEX [IX_GraphEvidence_SourceEventId] ON [dbo].[graph_evidence]([SourceEventId]);
END
");

            return;
        }

        dbContext.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS graph_nodes (
    NodeId TEXT NOT NULL PRIMARY KEY,
    Type TEXT NOT NULL,
    CanonicalLabel TEXT NOT NULL,
    AliasesJson TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    UpdatedUtc TEXT NOT NULL,
    Salience REAL NOT NULL
);

CREATE TABLE IF NOT EXISTS graph_edges (
    EdgeId TEXT NOT NULL PRIMARY KEY,
    FromId TEXT NOT NULL,
    ToId TEXT NOT NULL,
    Type TEXT NOT NULL,
    Weight REAL NOT NULL,
    CreatedUtc TEXT NOT NULL,
    UpdatedUtc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS graph_evidence (
    EvidenceId TEXT NOT NULL PRIMARY KEY,
    NodeId TEXT NULL,
    EdgeId TEXT NULL,
    SourceEventId TEXT NOT NULL,
    Snippet TEXT NOT NULL,
    Confidence REAL NOT NULL,
    CreatedUtc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS graph_projection_state (
    ProjectionName TEXT NOT NULL PRIMARY KEY,
    LastAppliedSeq INTEGER NOT NULL,
    UpdatedUtc TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_GraphNodes_Type ON graph_nodes(Type);
CREATE INDEX IF NOT EXISTS IX_GraphNodes_UpdatedUtc ON graph_nodes(UpdatedUtc);
CREATE INDEX IF NOT EXISTS IX_GraphEdges_FromToType ON graph_edges(FromId, ToId, Type);
CREATE INDEX IF NOT EXISTS IX_GraphEdges_UpdatedUtc ON graph_edges(UpdatedUtc);
CREATE INDEX IF NOT EXISTS IX_GraphEvidence_NodeId ON graph_evidence(NodeId);
CREATE INDEX IF NOT EXISTS IX_GraphEvidence_EdgeId ON graph_evidence(EdgeId);
CREATE INDEX IF NOT EXISTS IX_GraphEvidence_SourceEventId ON graph_evidence(SourceEventId);
");
    }

    private static async Task EnsureModeSchemaAsync(ChatDbContext dbContext)
    {
        if (dbContext.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true)
        {
            await dbContext.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID('dbo.mode_state', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[mode_state](
        [UserId] NVARCHAR(128) NOT NULL,
        [DeviceId] NVARCHAR(128) NOT NULL,
        [CurrentMode] NVARCHAR(32) NOT NULL,
        [PreviousMode] NVARCHAR(32) NULL,
        [LastConfidence] FLOAT NOT NULL,
        [LastSource] NVARCHAR(32) NOT NULL,
        [LastEvidenceEventIdsJson] NVARCHAR(MAX) NOT NULL,
        [UpdatedUtc] DATETIMEOFFSET NOT NULL,
        CONSTRAINT [PK_mode_state] PRIMARY KEY ([UserId], [DeviceId])
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ModeState_UpdatedUtc' AND object_id = OBJECT_ID('dbo.mode_state'))
BEGIN
    CREATE INDEX [IX_ModeState_UpdatedUtc] ON [dbo].[mode_state]([UpdatedUtc]);
END
");

            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS mode_state (
    UserId TEXT NOT NULL,
    DeviceId TEXT NOT NULL,
    CurrentMode TEXT NOT NULL,
    PreviousMode TEXT NULL,
    LastConfidence REAL NOT NULL,
    LastSource TEXT NOT NULL,
    LastEvidenceEventIdsJson TEXT NOT NULL,
    UpdatedUtc TEXT NOT NULL,
    PRIMARY KEY (UserId, DeviceId)
);
CREATE INDEX IF NOT EXISTS IX_ModeState_UpdatedUtc ON mode_state(UpdatedUtc);
");
    }

    private static void EnsureModeSchema(ChatDbContext dbContext)
    {
        if (dbContext.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true)
        {
            dbContext.Database.ExecuteSqlRaw(@"
IF OBJECT_ID('dbo.mode_state', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[mode_state](
        [UserId] NVARCHAR(128) NOT NULL,
        [DeviceId] NVARCHAR(128) NOT NULL,
        [CurrentMode] NVARCHAR(32) NOT NULL,
        [PreviousMode] NVARCHAR(32) NULL,
        [LastConfidence] FLOAT NOT NULL,
        [LastSource] NVARCHAR(32) NOT NULL,
        [LastEvidenceEventIdsJson] NVARCHAR(MAX) NOT NULL,
        [UpdatedUtc] DATETIMEOFFSET NOT NULL,
        CONSTRAINT [PK_mode_state] PRIMARY KEY ([UserId], [DeviceId])
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ModeState_UpdatedUtc' AND object_id = OBJECT_ID('dbo.mode_state'))
BEGIN
    CREATE INDEX [IX_ModeState_UpdatedUtc] ON [dbo].[mode_state]([UpdatedUtc]);
END
");

            return;
        }

        dbContext.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS mode_state (
    UserId TEXT NOT NULL,
    DeviceId TEXT NOT NULL,
    CurrentMode TEXT NOT NULL,
    PreviousMode TEXT NULL,
    LastConfidence REAL NOT NULL,
    LastSource TEXT NOT NULL,
    LastEvidenceEventIdsJson TEXT NOT NULL,
    UpdatedUtc TEXT NOT NULL,
    PRIMARY KEY (UserId, DeviceId)
);
CREATE INDEX IF NOT EXISTS IX_ModeState_UpdatedUtc ON mode_state(UpdatedUtc);
");
    }

    private static async Task EnsureNyphosPreferenceSchemaAsync(ChatDbContext dbContext)
    {
        if (dbContext.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true)
        {
            await dbContext.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID('dbo.nyphos_preferences', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[nyphos_preferences](
        [UserId] NVARCHAR(128) NOT NULL,
        [DeviceId] NVARCHAR(128) NOT NULL,
        [Tone] NVARCHAR(16) NOT NULL,
        [MutedUntilUtc] DATETIMEOFFSET NULL,
        [UpdatedUtc] DATETIMEOFFSET NOT NULL,
        CONSTRAINT [PK_nyphos_preferences] PRIMARY KEY ([UserId], [DeviceId])
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_NyphosPreferences_UpdatedUtc' AND object_id = OBJECT_ID('dbo.nyphos_preferences'))
BEGIN
    CREATE INDEX [IX_NyphosPreferences_UpdatedUtc] ON [dbo].[nyphos_preferences]([UpdatedUtc]);
END
");

            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS nyphos_preferences (
    UserId TEXT NOT NULL,
    DeviceId TEXT NOT NULL,
    Tone TEXT NOT NULL,
    MutedUntilUtc TEXT NULL,
    UpdatedUtc TEXT NOT NULL,
    PRIMARY KEY (UserId, DeviceId)
);
CREATE INDEX IF NOT EXISTS IX_NyphosPreferences_UpdatedUtc ON nyphos_preferences(UpdatedUtc);
");
    }

    private static void EnsureNyphosPreferenceSchema(ChatDbContext dbContext)
    {
        if (dbContext.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true)
        {
            dbContext.Database.ExecuteSqlRaw(@"
IF OBJECT_ID('dbo.nyphos_preferences', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[nyphos_preferences](
        [UserId] NVARCHAR(128) NOT NULL,
        [DeviceId] NVARCHAR(128) NOT NULL,
        [Tone] NVARCHAR(16) NOT NULL,
        [MutedUntilUtc] DATETIMEOFFSET NULL,
        [UpdatedUtc] DATETIMEOFFSET NOT NULL,
        CONSTRAINT [PK_nyphos_preferences] PRIMARY KEY ([UserId], [DeviceId])
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_NyphosPreferences_UpdatedUtc' AND object_id = OBJECT_ID('dbo.nyphos_preferences'))
BEGIN
    CREATE INDEX [IX_NyphosPreferences_UpdatedUtc] ON [dbo].[nyphos_preferences]([UpdatedUtc]);
END
");

            return;
        }

        dbContext.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS nyphos_preferences (
    UserId TEXT NOT NULL,
    DeviceId TEXT NOT NULL,
    Tone TEXT NOT NULL,
    MutedUntilUtc TEXT NULL,
    UpdatedUtc TEXT NOT NULL,
    PRIMARY KEY (UserId, DeviceId)
);
CREATE INDEX IF NOT EXISTS IX_NyphosPreferences_UpdatedUtc ON nyphos_preferences(UpdatedUtc);
");
    }
}
