using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Quantum.Mobile.Internal;

public static class MobileJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        MaxDepth = 48,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };
    public static JsonElement Element<T>(T value) => JsonSerializer.SerializeToElement(value, Options);
    public static T Read<T>(JsonElement value) => value.Deserialize<T>(Options) ?? throw new ArgumentException("A non-null payload is required.");
}
