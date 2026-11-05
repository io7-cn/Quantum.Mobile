using System.Text.Json;

namespace Quantum.MobileApp.Components.Pages;

public sealed partial class MainPage
{
    private async Task SignOutAsync()
    {
        Suspend();
        if (_pollTask is not null)
        {
            await _pollTask;
        }

        try
        {
            if (_client is not null)
            {
                await _client.CallAsync<JsonElement>("revoke");
            }
        }
        catch (Exception)
        {
        }

        if (_storageKey.Length > 0)
        {
            SecureStorage.Default.Remove(_storageKey);
            Preferences.Default.Remove(_storageKey + ".pending");
            SecureStorage.Default.Remove(_storageKey + ".outbox");
        }

        if (_authentication is not null)
        {
            await _authentication.LogoutAsync();
        }

        _client = null;
        _snapshot = null;
        _renderer = null;
        _pendingId = null;
        _outbox = null;
        _actionFeedback.IsVisible = false;
        _uncertain = false;
        _desktopId = null;
        _currentDesktop = null;
        _knownDesktops = [];
        _failedDraft = null;
        if (_restoreDraft is not null)
        {
            _restoreDraft.IsVisible = false;
        }

        _status.Text = "";
        _desktopStatus.Text = "";
        _pageCache.Clear();
        _deferredSnapshot = null;
        ShowSignIn();
    }
}
