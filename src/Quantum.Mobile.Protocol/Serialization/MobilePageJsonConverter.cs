using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Quantum.Mobile.Internal;

internal sealed class MobilePageJsonConverter : JsonConverter<MobilePage>
{
    public override MobilePage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var payload = JsonSerializer.Deserialize(ref reader, (JsonTypeInfo<Payload>)options.GetTypeInfo(typeof(Payload)));
        return payload is null ? null : new MobilePage(payload.Id, payload.Title, payload.Root) { SchemaVersion = payload.SchemaVersion };
    }

    public override void Write(Utf8JsonWriter writer, MobilePage value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, new Payload { Id = value.Id, Title = value.Title, Root = value.Root, SchemaVersion = value.SchemaVersion },
            (JsonTypeInfo<Payload>)options.GetTypeInfo(typeof(Payload)));

    internal sealed class Payload
    {
        public string Id { get; set; } = null!;
        public string Title { get; set; } = null!;
        public MobileNode Root { get; set; } = null!;
        public int SchemaVersion { get; set; } = 1;
    }
}
