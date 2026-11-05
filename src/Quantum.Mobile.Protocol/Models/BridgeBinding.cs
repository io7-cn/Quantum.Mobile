using System.Text.Json;

namespace Quantum.Mobile.Internal;

public sealed record BridgeBinding(string DesktopId, string MobileId, string MobileName, bool Approved);
