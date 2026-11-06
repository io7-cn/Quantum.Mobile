using Quantum.Mobile.Core.Actions;
using Quantum.Mobile.Internal;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Quantum.Mobile.SerializationSmoke;

internal static class Program
{
    private static readonly Uri Platform = new("https://platform.example/");

    private static async Task Main()
    {
        if (JsonSerializer.IsReflectionEnabledByDefault)
        {
            throw new InvalidOperationException("This check must run without reflection-based JSON metadata.");
        }

        var clock = new Clock();
        var handler = new ReplyHandler(clock);
        using var http = new HttpClient(handler);
        using var parameters = JsonDocument.Parse("{}");
        var failure = await PlatformRpcClient.SendAsync(http, Platform, "Failure", parameters.RootElement, null, default);
        if (failure.IsSuccess || failure.ErrorCode != "invalid_credentials")
        {
            throw new InvalidOperationException("The NOF failure envelope was not preserved.");
        }
        var nullFailure = await PlatformRpcClient.SendAsync(http, Platform, "FailureNull", parameters.RootElement, null, default);
        Require(!nullFailure.IsSuccess && nullFailure.Extra["trace"] == "fixture", "Failure metadata and a null payload must be supported.");
        var unauthorized = await PlatformRpcClient.SendAsync(http, Platform, "HttpUnauthorized", parameters.RootElement, null, default);
        Require(!unauthorized.IsSuccess && unauthorized.ErrorCode == "401", "HTTP authentication errors must retain their status.");
        var rpcFailure = await PlatformRpcClient.SendAsync(http, Platform, "RpcFailure", parameters.RootElement, null, default);
        Require(!rpcFailure.IsSuccess && rpcFailure.ErrorCode == "-32000", "JSON-RPC errors must retain their code.");

        string? saved = null;
        PlatformSession NewSession() => new(http, Platform, () => Task.FromResult(saved), value =>
        {
            saved = value;
            return Task.CompletedTask;
        }, () =>
        {
            saved = null;
            return Task.CompletedTask;
        }, clock);

        var session = NewSession();
        await session.LoginAsync(" native@example.test ", "fixture-password");
        Require(session.IsAuthenticated && session.User?.Username == "native-test" && saved is not null, "Login must deserialize and persist the complete session.");
        Require(await NewSession().RestoreAsync(), "The saved session must restore without reflection.");
        clock.Advance(TimeSpan.FromHours(2));
        Require(await session.ExecuteAsync((token, _) => Task.FromResult(token)) == "refreshed-access" && handler.RefreshCalls == 1, "Expired access must refresh and persist its replacement.");
        Require(await NewSession().RestoreAsync(), "The refreshed session must survive restart.");
        await session.LogoutAsync();
        Require(saved is null && !session.IsAuthenticated, "Logout must clear the saved session and accept a response without value.");
        saved = "header." + Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"exp\":1893456000}")).TrimEnd('=').Replace('+', '-').Replace('/', '_') + ".signature";
        Require(await NewSession().RestoreAsync(), "Legacy token restoration must deserialize GetCurrentUser.");

        var bridge = new BridgeClient(http, Platform, () => "fixture-access");
        bridge.Registration = await bridge.CallAsync<BridgeRegistration>("register", new JsonObject { ["name"] = "Test phone", ["kind"] = "mobile", ["clientInstanceId"] = "instance" });
        Require(bridge.Registration.Device.Id == "phone", "Registration must deserialize without reflection.");
        var registration = JsonSerializer.Serialize(bridge.Registration, MobileJson.TypeInfo<BridgeRegistration>());
        Require(JsonSerializer.Deserialize(registration, MobileJson.TypeInfo<BridgeRegistration>())?.Device.Id == "phone", "Saved registration must survive restart.");
        await bridge.CallAsync<JsonElement>("heartbeat", new JsonObject { ["clientInstanceId"] = "instance" });
        Require((await bridge.CallAsync<BridgeDevice[]>("devices")).Length == 1, "Device discovery must deserialize its array.");
        Require((await bridge.CallAsync<BridgePageInfo[]>("pages", new JsonObject { ["desktopId"] = "desktop", ["includeIcons"] = true, ["includeIconImages"] = true })).Single().Icon == "N", "Page catalogs must retain optional icons.");
        var messages = await bridge.CallAsync<BridgeMessage[]>("poll");
        var snapshot = MobileJson.Read<BridgeSnapshot>(messages.Single().Body);
        Require(snapshot.Page.SchemaVersion == 2 && MobileUiValidator.Validate(snapshot.Page, wire: true).ContainsKey("files"), "Schema-2 snapshots must retain attachment controls.");
        using var legacyPage = JsonDocument.Parse("""{"id":"legacy","title":"Legacy","root":{"type":"Text","id":"title","text":"Hello"}}""");
        var schemaOne = MobileJson.Read<MobilePage>(legacyPage.RootElement);
        Require(schemaOne.SchemaVersion == 1 && schemaOne.Root.Style == "body" && schemaOne.Root.MaxLength == 500 && schemaOne.Root.Enabled && MobileUiValidator.Validate(schemaOne, wire: true).Count == 1, "Omitted page and node members must keep the existing wire defaults.");
        await bridge.CallAsync<JsonElement>("ack", new BridgeAck(messages[0].Id, messages[0].Lease!));
        var file = new BridgeAttachment(new string('a', 32), "sample.txt", "text/plain", 3, new string('b', 64));
        var files = MobileAttachments.Read(MobileJson.Element(new[] { file }));
        Require(files.Length == 1, "Attachment metadata must deserialize without reflection.");
        await bridge.CallAsync<JsonElement>("attachment/create", new JsonObject { ["desktopId"] = "desktop", ["viewId"] = snapshot.ViewId, ["inputId"] = "files", ["attachment"] = JsonSerializer.SerializeToNode(file, MobileJson.TypeInfo<BridgeAttachment>()) });
        await bridge.CallAsync<JsonElement>("attachment/write", new JsonObject { ["attachmentId"] = file.Id, ["offset"] = 0, ["dataBase64"] = "YWJj" });
        await bridge.CallAsync<JsonElement>("attachment/complete", new JsonObject { ["attachmentId"] = file.Id });
        var inputs = new Dictionary<string, JsonElement> { ["files"] = MobileJson.Element(files) };
        var send = new BridgeSend("command", "desktop", "action", MobileJson.Element(new BridgeInvoke(snapshot.ViewId, "save-action", inputs)));
        var pending = new PendingAction(send, "phone", "sample.notes", "notes", snapshot.ExpiresAt, inputs);
        var pendingJson = JsonSerializer.Serialize(pending, ClientJsonContext.Default.PendingAction);
        Require(JsonSerializer.Deserialize(pendingJson, ClientJsonContext.Default.PendingAction)?.Inputs["files"].GetArrayLength() == 1, "Saved actions and failed drafts must retain their fields.");
        await bridge.CallAsync<JsonElement>("send", send);
        Require((await bridge.CallAsync<BridgeOutcome>("status", new JsonObject { ["requestId"] = "command" })).Status == "succeeded", "Action outcomes must deserialize without reflection.");
        var order = new[] { "sample.notes/notes" };
        Require(JsonSerializer.Deserialize(JsonSerializer.Serialize(order, MobileJson.TypeInfo<string[]>()), MobileJson.TypeInfo<string[]>())?.Single() == order[0], "Saved navigation must survive restart.");
        await bridge.CallAsync<JsonElement>("revoke");
        Console.WriteLine("Reflection-disabled login, refresh, restore, logout, bridge, schema-2 attachments and persistence checks passed.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class Clock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan value) => _now += value;
    }

    private sealed class ReplyHandler(Clock clock) : HttpMessageHandler
    {
        private const string User = """{"userId":"native-user","username":"native-test","email":"native@example.test","roles":0}""";
        private const string Device = """{"id":"phone","name":"Test phone","kind":"mobile","seenAt":"2026-11-05T12:00:00Z","sessionId":"session"}""";
        private const string Page = """{"id":"notes","title":"Notes","schemaVersion":2,"root":{"type":"VStack","id":"root","children":[{"type":"Attachments","id":"files"},{"type":"Button","id":"save","text":"Save","actionId":"save-action","inputIds":["files"]}]}}""";
        public int RefreshCalls { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var id = body.RootElement.GetProperty("id").GetString();
            var method = body.RootElement.GetProperty("method").GetString();
            var parameters = body.RootElement.GetProperty("params");
            if (method == "HttpUnauthorized")
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("Unauthorized") };
            }
            if (method == "RpcFailure")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"jsonrpc\":\"2.0\",\"id\":\"" + id + "\",\"error\":{\"code\":-32000,\"message\":\"Rejected.\"}}", Encoding.UTF8, "application/json")
                };
            }
            string result;
            if (method is "Failure" or "FailureNull")
            {
                result = "\"isSuccess\":false,\"errorCode\":\"invalid_credentials\",\"message\":\"Rejected.\",\"extra\":{\"trace\":\"fixture\"}" + (method == "FailureNull" ? ",\"value\":null" : "");
            }
            else if (method is "Login" or "RefreshLogin")
            {
                if (method == "Login")
                {
                    Require(parameters.GetProperty("email").GetString() == "native@example.test" && parameters.GetProperty("password").GetString() == "fixture-password", "Login request fields changed.");
                }
                else
                {
                    Require(parameters.GetProperty("refreshToken").GetString() == "fixture-refresh" && parameters.GetProperty("requestId").GetGuid() != Guid.Empty, "Refresh request fields changed.");
                    RefreshCalls++;
                }
                var access = method == "Login" ? "fixture-access" : "refreshed-access";
                result = "\"isSuccess\":true,\"value\":{\"accessToken\":\"" + access + "\",\"expiresAtUtc\":\"" + clock.GetUtcNow().UtcDateTime.AddHours(1).ToString("O") + "\",\"refreshToken\":\"fixture-refresh\",\"refreshExpiresAtUtc\":\"2030-01-01T00:00:00Z\",\"user\":" + User + "}";
            }
            else if (method == "GetCurrentUser")
            {
                result = "\"isSuccess\":true,\"value\":" + User;
            }
            else if (method == "Logout")
            {
                result = "\"isSuccess\":true";
            }
            else
            {
                Require(method == "ExchangeMobileBridge" && request.RequestUri!.AbsolutePath == "/rpc/mobile", "Bridge endpoint or method changed.");
                var payload = parameters.GetProperty("operation").GetString() switch
                {
                    "register" => "{\"device\":" + Device + ",\"secret\":\"fixture-secret\"}",
                    "devices" => "[" + Device + "]",
                    "pages" => """[{"pluginId":"sample.notes","pageId":"notes","title":"Notes","icon":"N"}]""",
                    "poll" => "[{\"id\":\"message\",\"senderId\":\"desktop\",\"targetId\":\"phone\",\"kind\":\"snapshot\",\"lease\":\"lease\",\"expiresAt\":\"2026-11-05T15:00:00Z\",\"body\":{\"viewId\":\"view\",\"desktopSessionId\":\"desktop-session\",\"pluginId\":\"sample.notes\",\"runtimeId\":\"runtime\",\"expiresAt\":\"2026-11-05T15:00:00Z\",\"page\":" + Page + "}}]",
                    "status" => """{"requestId":"command","status":"succeeded","message":"Saved","result":null}""",
                    _ => "{}"
                };
                result = "\"isSuccess\":true,\"value\":" + payload;
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"jsonrpc\":\"2.0\",\"id\":\"" + id + "\",\"result\":{" + result + "}}", Encoding.UTF8, "application/json")
            };
        }
    }
}
