using NOF.Contract;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Quantum.Mobile.Internal;

// Result<T>'s JSON constructor is internal to NOF.Contract, so a consumer's
// generated context cannot call it. Recreate the result through its public factories.
internal sealed class PlatformResultJsonConverter : JsonConverter<Result<JsonElement>>
{
    public override Result<JsonElement> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var payload = JsonSerializer.Deserialize(ref reader, ClientJsonContext.Default.PlatformResultPayload)
            ?? throw new JsonException("Missing platform result.");
        return payload.IsSuccess ? Result.Success(payload.Value, payload.Extra)
            : Result.Fail(payload.ErrorCode ?? "", payload.Message ?? "", payload.Extra);
    }

    public override void Write(Utf8JsonWriter writer, Result<JsonElement> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteBoolean("isSuccess", value.IsSuccess);
        writer.WriteString("errorCode", value.ErrorCode);
        writer.WriteString("message", value.Message);
        writer.WritePropertyName("value");
        if (value.Value.ValueKind == JsonValueKind.Undefined)
        {
            writer.WriteNullValue();
        }
        else
        {
            value.Value.WriteTo(writer);
        }

        writer.WriteStartObject("extra");
        foreach (var (key, item) in value.Extra)
        {
            writer.WriteString(key, item);
        }
        writer.WriteEndObject();
        writer.WriteEndObject();
    }
}
