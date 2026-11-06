using Quantum.Mobile.Internal;
using System.Text.Json;

namespace Quantum.MobileApp.Components.Pages;

public sealed partial class MainPage
{
    private async Task ShowPagesAsync()
    {
        _language.IsEnabled = true;
        var pages = await _client!.CallAsync<BridgePageInfo[]>("pages", new JsonObject { ["desktopId"] = _desktopId, ["includeIcons"] = true, ["includeIconImages"] = true });
        _navigation.Replace(pages);
        RenderNavigation();
        if (pages.Length > 0)
        {
            var page = _navigation.Current ?? pages[0];
            await SelectNavigationPageAsync(page);
        }
        else
        {
            _snapshot = null;
            _renderer = null;
            _body.Children.Clear();
            _body.Children.Add(new Label { Text = T("这台电脑暂时没有移动插件页面", "No mobile plugin pages on this desktop yet"), FontSize = 24 });
            _body.Children.Add(MobileTheme.Caption(T("安装支持移动页面的插件后，可从设置中刷新插件列表。", "Install a plugin with mobile pages, then refresh the plugin list in Settings.")));
        }
    }

    private async Task OpenAsync(string pluginId, string pageId)
    {
        if (_renderer?.IsDirty == true && !await DisplayAlertAsync(T("刷新页面", "Refresh page"), T("丢弃未提交的输入？", "Discard unsubmitted input?"), T("刷新", "Refresh"), T("取消", "Cancel")))
        {
            return;
        }

        if (_navigation.Pages.Any(p => p.PluginId == pluginId && p.PageId == pageId))
        {
            _navigation.Select(pluginId, pageId);
        }

        await CloseNavigationAsync();
        RenderNavigation();
        _pageContent.IsEnabled = false;
        _awaitingSnapshot = true;
        _snapshot = null;
        _renderer = null;
        _deferredSnapshot = null;
        _body.Children.Clear();
        if (_pageCache.TryGetValue(pluginId + ":" + pageId, out var cached))
        {
            RenderSnapshot(cached, false);
            _pageContent.IsEnabled = false;
        }
        else
        {
            _body.Children.Add(new ActivityIndicator { IsRunning = true, Color = MobileTheme.Accent, HeightRequest = 32 });
        }

        await RequestSnapshotAsync(pluginId, pageId);
    }

    private async Task RequestSnapshotAsync(string pluginId, string pageId)
    {
        _openRequestId = Guid.NewGuid().ToString("N");
        _subscriptionRenewAt = DateTimeOffset.UtcNow.AddSeconds(_awaitingSnapshot ? 10 : 120);
        _status.Text = T("正在同步…", "Syncing…");
        await _client!.CallAsync<JsonElement>("send", new BridgeSend(_openRequestId, _desktopId!, "open", MobileJson.Element(new BridgeOpen(pluginId, pageId))));
    }

    private void ShowSnapshot(BridgeSnapshot snapshot)
    {
        if (_snapshot?.ViewId == snapshot.ViewId)
        {
            return;
        }

        if (!_preview && snapshot.RequestId != _openRequestId)
        {
            return;
        }

        if (_snapshot is not null && snapshot.RequestId == _snapshot.RequestId && snapshot.ExpiresAt < _snapshot.ExpiresAt)
        {
            return;
        }

        if (_pendingId is not null)
        {
            _deferredSnapshot = snapshot;
            return;
        }

        if (_awaitingSnapshot)
        {
            _subscriptionRenewAt = DateTimeOffset.UtcNow.AddMinutes(2);
        }

        _awaitingSnapshot = false;
        RenderSnapshot(snapshot, true);
        _refreshCompletion?.TrySetResult();
    }

    private BridgeSnapshot? _deferredSnapshot;
    private void RenderSnapshot(BridgeSnapshot snapshot, bool preserveInputs)
    {
        var samePage = _snapshot?.PluginId == snapshot.PluginId && _snapshot.Page.Id == snapshot.Page.Id;
        _snapshot = snapshot;
        _pageCache[snapshot.PluginId + ":" + snapshot.Page.Id] = snapshot;
        _status.Text = MobileFeedback.Snapshot(_uncertain, _pendingId is not null, Chinese);
        if (samePage && preserveInputs && _renderer is not null)
        {
            var root = _renderer.Update(snapshot.Page);
            if (!ReferenceEquals(_pageContent.Children[0], root))
            {
                _pageContent.Children.Clear();
                _pageContent.Children.Add(root);
            }
        }
        else
        {
            _renderer = new(InvokeAsync);
            _pageContent.Children.Clear();
            _pageContent.Children.Add(_renderer.Render(snapshot.Page));
            _body.Children.Clear();
            _body.Children.Add(_pageContent);
        }

        UpdateActionAvailability();
        if (_offerFailedDraft && _failedDraft is { } draft && snapshot.PluginId == draft.PluginId && snapshot.Page.Id == draft.PageId)
        {
            _offerFailedDraft = false;
            ShowNotice(T("上次未完成的输入已保留，可恢复后继续编辑。", "Your unsuccessful submission was saved. Restore it to continue editing."));
        }
    }
}
