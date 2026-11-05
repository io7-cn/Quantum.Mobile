using System.Text.Json;

namespace Quantum.Mobile.Internal;

public sealed record BridgeDevice(string Id, string Name, string Kind, DateTimeOffset SeenAt, string SessionId);
