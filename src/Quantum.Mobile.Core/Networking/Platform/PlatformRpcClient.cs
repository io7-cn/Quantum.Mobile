using NOF.Contract;
using System.Text.Json;

namespace Quantum.Mobile.Internal;
/// <summary>Sends unary Platform requests using the framework's JSON-RPC transport.</summary>
public static class PlatformRpcClient
{
    // NOF endpoints write failures as Result without a value, even for operations returning Result<T>.
    private static readonly JsonSerializerOptions TransportJson = CreateTransportJson();

    public static Task<Result<JsonElement>> SendAsync(HttpClient http, Uri platform, string method, JsonElement parameters, string? accessToken, CancellationToken cancellationToken)
    {
        KeyValuePair<string, string?>[] headers = string.IsNullOrWhiteSpace(accessToken) ? [] : [new("Authorization", $"Bearer {accessToken}")];
        return JsonRpcHttpClient.SendAsync(http, new Uri(platform, "rpc").AbsoluteUri, method, parameters, headers, TransportJson.GetRequiredTypeInfo<JsonElement>(), TransportJson.GetRequiredTypeInfo<Result<JsonElement>>(), cancellationToken);
    }

    private static JsonSerializerOptions CreateTransportJson()
    {
        var options = new JsonSerializerOptions(JsonSerializerOptions.NOF)
        {
            RespectRequiredConstructorParameters = false
        };
        options.Converters.Add(new PlatformResultJsonConverter());
        options.TypeInfoResolverChain.Insert(0, ClientJsonContext.Default);
        return options;
    }
}
