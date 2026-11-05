using System.Text.Json;

namespace Quantum.Mobile.Internal;

internal sealed record PlatformResultPayload(bool IsSuccess, string? ErrorCode, string? Message, JsonElement Value, Dictionary<string, string>? Extra);
