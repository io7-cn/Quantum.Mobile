using System.Text.Json;

namespace Quantum.Mobile.Internal;

public sealed record BridgeAck(string Id, string Lease);
