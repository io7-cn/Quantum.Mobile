using System.Text.Json;

namespace Quantum.Mobile.Internal;

public sealed record BridgeMessage(string Id, string SenderId, string TargetId, string Kind, JsonElement Body, DateTimeOffset ExpiresAt, string? Lease = null);
