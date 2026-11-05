namespace Quantum.Mobile.Debugging;

public interface IMobileDebugTarget
{
    Task<object> ExecuteAsync(string path, System.Text.Json.JsonElement body);
}
