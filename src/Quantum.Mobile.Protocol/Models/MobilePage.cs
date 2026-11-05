using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Quantum.Mobile.Internal;

public sealed record MobilePage(string Id, string Title, MobileNode Root)
{
    public int SchemaVersion { get; init; } = 1;
}
