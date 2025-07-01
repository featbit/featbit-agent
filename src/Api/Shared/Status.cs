namespace Api.Shared;

public record Status(
    string Serves,
    long DataVersion,
    string SyncState,
    DateTime? LastSyncedAt,
    DateTime ReportedAt
);

public record StatusSyncPayload(
    string AgentId,
    Status Status
);