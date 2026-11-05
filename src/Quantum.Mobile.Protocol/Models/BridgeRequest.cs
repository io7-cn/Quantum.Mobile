using System.Text.Json;

namespace Quantum.Mobile.Internal;

public sealed record BridgeRequest(string Operation, string? DeviceId = null, string? DeviceSecret = null, JsonElement Body = default);
