using System.Text.Json;

namespace Quantum.Mobile.Internal;

public sealed record BridgePageError(string RequestId, string Code, string Message);
