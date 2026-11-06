using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Quantum.Mobile.Internal;

// Source-generated constructors reset omitted init-only members. This mutable
// payload preserves the wire defaults before constructing the immutable node.
internal sealed class MobileNodeJsonConverter : JsonConverter<MobileNode>
{
    public override MobileNode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var payload = JsonSerializer.Deserialize(ref reader, (JsonTypeInfo<Payload>)options.GetTypeInfo(typeof(Payload)));
        return payload is null ? null : new MobileNode(payload.Type, payload.Id)
        {
            Text = payload.Text,
            Placeholder = payload.Placeholder,
            Style = payload.Style,
            MaxLength = payload.MaxLength,
            Enabled = payload.Enabled,
            ActionId = payload.ActionId,
            InputIds = payload.InputIds,
            Action = payload.Action,
            Children = payload.Children
        };
    }

    public override void Write(Utf8JsonWriter writer, MobileNode value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, new Payload
        {
            Type = value.Type,
            Id = value.Id,
            Text = value.Text,
            Placeholder = value.Placeholder,
            Style = value.Style,
            MaxLength = value.MaxLength,
            Enabled = value.Enabled,
            ActionId = value.ActionId,
            InputIds = value.InputIds,
            Action = value.Action,
            Children = value.Children
        }, (JsonTypeInfo<Payload>)options.GetTypeInfo(typeof(Payload)));

    internal sealed class Payload
    {
        public string Type { get; set; } = null!;
        public string Id { get; set; } = null!;
        public string? Text { get; set; }
        public string? Placeholder { get; set; }
        public string Style { get; set; } = "body";
        public int MaxLength { get; set; } = 500;
        public bool Enabled { get; set; } = true;
        public string? ActionId { get; set; }
        public string[] InputIds { get; set; } = [];
        public MobileRpcAction? Action { get; set; }
        public MobileNode[] Children { get; set; } = [];
    }
}
