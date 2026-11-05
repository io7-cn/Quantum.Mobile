using Quantum.Mobile.Internal;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Quantum.MobileApp.Components.Pages;

public sealed partial class MainPage
{
    private async Task SignInAsync()
    {
        var uri = PlatformAddress.Parse(_baseUrl);
        _authentication = CreateAuthentication(uri);
        try
        {
            await _authentication.LoginAsync(_email.Text ?? "", _password.Text ?? "");
        }
        finally
        {
            _password.Text = "";
        }

        _accountAccepted = true;
        await ConnectSessionAsync(uri);
    }

    private async Task ConnectSessionAsync(Uri uri)
    {
        var userId = _authentication!.User!.UserId;
        _storageKey = "quantum.mobile." + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(uri.AbsoluteUri + userId)));
        _client = new(_http, uri, () => _authentication.AccessToken)
        {
            Authentication = _authentication
        };
        var instance = Preferences.Default.Get(_storageKey + ".instance", "");
        if (instance.Length == 0)
        {
            instance = Guid.NewGuid().ToString("N");
            Preferences.Default.Set(_storageKey + ".instance", instance);
        }

        var saved = await SecureStorage.Default.GetAsync(_storageKey);
        if (saved is not null)
        {
            _client.Registration = JsonSerializer.Deserialize<BridgeRegistration>(saved, MobileJson.Options);
            try
            {
                await _client.CallAsync<JsonElement>("heartbeat", new { clientInstanceId = instance });
            }
            catch (BridgeException e) when (e.Code == "mobile_device_revoked")
            {
                _client.Registration = null;
            }
        }

        if (_client.Registration is null)
        {
            _client.Registration = await _client.CallAsync<BridgeRegistration>("register", new { name = DeviceInfo.Current.Name, kind = "mobile", clientInstanceId = instance });
            await SecureStorage.Default.SetAsync(_storageKey, JsonSerializer.Serialize(_client.Registration, MobileJson.Options));
        }

        _pendingId = Preferences.Default.Get(_storageKey + ".pending", (string?)null);
        var queued = await SecureStorage.Default.GetAsync(_storageKey + ".outbox");
        _outbox = queued is null ? null : JsonSerializer.Deserialize<PendingAction>(queued, MobileJson.Options);
        if (_outbox is not null)
        {
            _pendingId = _outbox.Send.Id;
            SetActionFeedback(T("正在恢复待发送操作…", "Recovering pending action…"));
        }

        var failed = await SecureStorage.Default.GetAsync(_storageKey + ".failed-draft");
        _failedDraft = failed is null ? null : JsonSerializer.Deserialize<PendingAction>(failed, MobileJson.Options);
        _offerFailedDraft = _failedDraft is not null;
        if (_failedDraft is not null)
        {
            EnsureDraftRecovery();
        }

        _preview = false;
        Preferences.Default.Set("quantum.mobile.email", _email.Text?.Trim() ?? "");
        await EnterDesktopAsync();
        Resume();
    }
}
