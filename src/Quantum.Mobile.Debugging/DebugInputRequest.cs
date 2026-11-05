namespace Quantum.Mobile.Debugging;

public sealed class DebugInputRequest : DebugViewRequest
{
    public required string NodeId { get; set; }
    public required System.Text.Json.JsonElement Value { get; set; }
}
