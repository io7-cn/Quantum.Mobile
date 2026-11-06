namespace Quantum.Mobile.Internal;

internal sealed record BridgeRpcRequest(string Jsonrpc, string Id, string Method, BridgeRequest Params);
