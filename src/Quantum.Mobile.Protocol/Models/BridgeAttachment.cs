using System.Text.Json;
using System.Text.RegularExpressions;

namespace Quantum.Mobile.Internal;

public sealed record BridgeAttachment(string Id, string Name, string ContentType, long Size, string Sha256);
