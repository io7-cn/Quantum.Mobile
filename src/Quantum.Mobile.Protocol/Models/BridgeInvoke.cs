using System.Text.Json;

namespace Quantum.Mobile.Internal;

public sealed record BridgeInvoke(string ViewId, string ActionId, Dictionary<string, JsonElement> Inputs);
