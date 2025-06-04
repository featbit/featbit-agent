using System.Text.Json.Nodes;
using Domain.Shared;

namespace Api.Store;

public record DataSetSnapshot(string EventType, DataSetSnapshotItem[] Items);

public record DataSetSnapshotItem(
    Guid EnvId,
    SecretWithValue[] Secrets,
    JsonObject[] FeatureFlags,
    JsonObject[] Segments);