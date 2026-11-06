using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Quantum.Mobile.Internal;

public static class MobileJson
{
    public static readonly JsonSerializerOptions Options = CreateOptions();

    public static JsonTypeInfo<T> TypeInfo<T>() => (JsonTypeInfo<T>)Options.GetTypeInfo(typeof(T));
    public static JsonElement Element<T>(T value) => JsonSerializer.SerializeToElement(value, TypeInfo<T>());
    public static T Read<T>(JsonElement value) => value.Deserialize(TypeInfo<T>()) ?? throw new ArgumentException("A non-null payload is required.");

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            MaxDepth = 48,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            TypeInfoResolver = MobileJsonContext.Default
        };
        options.Converters.Add(new MobileNodeJsonConverter());
        options.Converters.Add(new MobilePageJsonConverter());
        if (JsonSerializer.IsReflectionEnabledByDefault)
        {
#pragma warning disable IL2026, IL3050 // Keep JIT-only callers compatible; trimmed clients use the generated context.
            options.TypeInfoResolverChain.Add(new DefaultJsonTypeInfoResolver());
#pragma warning restore IL2026, IL3050
        }

        return options;
    }
}
