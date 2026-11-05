using NOF.Contract;
using Quantum.Mobile.Core.Actions;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Quantum.Mobile.Internal;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, GenerationMode = JsonSourceGenerationMode.Metadata, RespectRequiredConstructorParameters = false,
    Converters = [typeof(PlatformResultJsonConverter)])]
[JsonSerializable(typeof(Result<JsonElement>))]
[JsonSerializable(typeof(PlatformResultPayload))]
[JsonSerializable(typeof(JsonObject))]
[JsonSerializable(typeof(PlatformSessionState))]
[JsonSerializable(typeof(PlatformSessionUser))]
[JsonSerializable(typeof(BridgeRpcRequest))]
[JsonSerializable(typeof(PendingAction))]
internal partial class ClientJsonContext : JsonSerializerContext;
