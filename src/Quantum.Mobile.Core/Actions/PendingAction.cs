using Quantum.Mobile.Internal;
using System.Text.Json;

namespace Quantum.Mobile.Core.Actions;

public sealed record PendingAction(BridgeSend Send, string MobileId, string PluginId, string PageId, DateTimeOffset ExpiresAt, Dictionary<string, JsonElement> Inputs);
