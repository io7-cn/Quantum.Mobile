namespace Quantum.Mobile.Debugging;

public sealed class DebugOpenRequest
{
    public required string PluginId { get; set; }
    public required string PageId { get; set; }
    public bool DiscardDraft { get; set; }
}
