using System.Text.Json;

namespace Quantum.Mobile.Internal;

public sealed record BridgeRegistration(BridgeDevice Device, string Secret);
