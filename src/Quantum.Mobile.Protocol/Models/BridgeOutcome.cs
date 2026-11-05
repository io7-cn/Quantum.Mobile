using System.Text.Json;

namespace Quantum.Mobile.Internal;

public sealed record BridgeOutcome(string RequestId, string Status, string Message, JsonElement? Result = null);
