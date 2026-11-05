using System.Text.Json;

namespace Quantum.Mobile.Internal;

public sealed record BridgeSend(string Id, string TargetId, string Kind, JsonElement Body);
