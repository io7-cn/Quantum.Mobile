using System.Text.Json;

namespace Quantum.Mobile.Internal;

public sealed record BridgeSnapshot(string ViewId, string DesktopSessionId, string PluginId, string RuntimeId, DateTimeOffset ExpiresAt, MobilePage Page, string? RequestId = null);
