using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Quantum.Mobile.Internal;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, GenerationMode = JsonSourceGenerationMode.Metadata,
    Converters = [typeof(MobileNodeJsonConverter), typeof(MobilePageJsonConverter)])]
[JsonSerializable(typeof(MobileNodeJsonConverter.Payload), TypeInfoPropertyName = "NodePayload")]
[JsonSerializable(typeof(MobilePageJsonConverter.Payload), TypeInfoPropertyName = "PagePayload")]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(JsonObject))]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(byte))]
[JsonSerializable(typeof(sbyte))]
[JsonSerializable(typeof(short))]
[JsonSerializable(typeof(ushort))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(uint))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(ulong))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(decimal))]
[JsonSerializable(typeof(BridgeRequest))]
[JsonSerializable(typeof(BridgeRegistration))]
[JsonSerializable(typeof(BridgeDevice[]))]
[JsonSerializable(typeof(BridgeBinding[]))]
[JsonSerializable(typeof(BridgePageInfo[]))]
[JsonSerializable(typeof(BridgeSnapshot))]
[JsonSerializable(typeof(BridgeMessage[]))]
[JsonSerializable(typeof(BridgeSend))]
[JsonSerializable(typeof(BridgeInvoke))]
[JsonSerializable(typeof(BridgeOutcome))]
[JsonSerializable(typeof(BridgeOpen))]
[JsonSerializable(typeof(BridgePageError))]
[JsonSerializable(typeof(BridgeAck))]
[JsonSerializable(typeof(BridgeAttachment[]))]
internal partial class MobileJsonContext : JsonSerializerContext;
