using System.Text.Json;

namespace Quantum.Mobile.Internal;

public sealed record BridgePageInfo(string PluginId, string PageId, string Title)
{
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? Icon { get; init; }

    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? IconImage { get; init; }
}
