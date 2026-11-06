using Quantum.Mobile.Internal;
using System.Text.Json;

namespace Quantum.MobileApp.Components.Pages;

public sealed partial class MainPage
{
    public void Suspend()
    {
        _resumeRequested = false;
        if (_renderer is { } renderer)
        {
            _ = renderer.StopRecordingAsync();
        }

        _refreshCompletion?.TrySetCanceled();
        _polling?.Cancel();
        _connected = false;
        _pageContent.IsEnabled = false;
    }

    public void Resume()
    {
#if DEBUG
        if (_debugPreviewStartup)
        {
            return;
        }
#endif
        if (_preview)
        {
            return;
        }

        if (_client is null)
        {
            _ = RestoreLoginAsync(true);
            return;
        }

        _resumeRequested = true;
        _ = ResumeAsync();
    }

    private async Task ResumeAsync()
    {
        await _resumeGate.WaitAsync();
        try
        {
            if (_pollTask is { IsCompleted: false })
            {
                _polling?.Cancel();
                try
                {
                    await _pollTask;
                }
                catch (OperationCanceledException) when (_polling?.IsCancellationRequested == true)
                {
                }
            }

            if (!_resumeRequested || _client is null || _preview)
            {
                return;
            }
            _polling?.Dispose();
            _polling = new();
            _pollTask = PollAsync(_polling.Token);
        }
        finally
        {
            _resumeGate.Release();
        }
    }

    private async Task PollAsync(CancellationToken token)
    {
        var client = _client;
        if (client is null)
        {
            return;
        }

        if (!await _pollGate.WaitAsync(0, token))
        {
            return;
        }

        var delay = .5;
        var nextDevices = DateTimeOffset.MinValue;
        var nextStatus = DateTimeOffset.MinValue;
        try
        {
            while (!token.IsCancellationRequested && ReferenceEquals(_client, client))
            {
                try
                {
                    var messages = await client.CallAsync<BridgeMessage[]>("poll", cancellationToken: token);
                    token.ThrowIfCancellationRequested();
                    if (!ReferenceEquals(_client, client))
                    {
                        break;
                    }

                    foreach (var message in messages)
                    {
                        if (message.SenderId == _desktopId && message.Kind == "snapshot")
                        {
                            ShowSnapshot(MobileJson.Read<BridgeSnapshot>(message.Body));
                        }

                        if (message.SenderId == _desktopId && message.Kind == "pageError")
                        {
                            var error = MobileJson.Read<BridgePageError>(message.Body);
                            if (error.RequestId == _openRequestId)
                            {
                                _status.Text = MobileFeedback.Failure(error.Code, Chinese);
                                ShowNotice(_status.Text ?? "");
                                _refreshCompletion?.TrySetException(new BridgeException(error.Code, _status.Text ?? "Page unavailable."));
                            }
                        }

                        if (message.Kind == "result")
                        {
                            Outcome(MobileJson.Read<BridgeOutcome>(message.Body));
                        }

                        await client.CallAsync<JsonElement>("ack", new BridgeAck(message.Id, message.Lease!), token);
                    }

                    if (_pendingId is not null && DateTimeOffset.UtcNow >= nextStatus)
                    {
                        nextStatus = DateTimeOffset.UtcNow.AddSeconds(2);
                        try
                        {
                            Outcome(await client.CallAsync<BridgeOutcome>("status", new JsonObject { ["requestId"] = _pendingId }, token));
                        }
                        catch (BridgeException e) when (e.Code == "mobile_not_found")
                        {
                            if (_outbox is not null)
                            {
                                await FlushOutboxAsync(token);
                            }
                            else
                            {
                                _uncertain = true;
                                ShowNotice(T("请在电脑核对之前的操作", "Check the previous action on desktop"));
                            }
                        }
                    }

                    if (DateTimeOffset.UtcNow >= nextDevices || _snapshot is null && !_awaitingSnapshot && _pendingId is null && !_uncertain)
                    {
                        nextDevices = DateTimeOffset.UtcNow.AddSeconds(10);
                        var desktops = await client.CallAsync<BridgeDevice[]>("devices", cancellationToken: token);
                        _knownDesktops = desktops.Where(d => d.Kind == "desktop").ToArray();
                        _currentDesktop = _knownDesktops.FirstOrDefault(d => d.Id == _desktopId);
                        _connected = _currentDesktop is not null && MobileDesktopSelection.IsOnline(_currentDesktop, client.UtcNow);
                        if (_snapshot is null && !_awaitingSnapshot && !_selectingDesktop && _pendingId is null && !_uncertain)
                        {
                            var available = MobileDesktopSelection.Choose(_knownDesktops, _desktopId, client.UtcNow, Preferences.Default.Get(_storageKey + ".desktop-name", ""));
                            if (available is not null && MobileDesktopSelection.IsOnline(available, client.UtcNow))
                            {
                                await SelectDesktopAsync(available, false);
                            }
                        }

                        _currentDesktopState.Text = _connected ? T("在线", "Online") : T("未连接", "Not connected");
                        if (_menuExpanded)
                        {
                            RenderDesktopChoices();
                        }

                        _desktopStatus.Text = (_connected ? T("电脑在线 · ", "Desktop online · ") : T("电脑离线 · ", "Desktop offline · ")) + DateTime.Now.ToString("HH:mm:ss");
                    }

                    if (_snapshot is { } current && _pendingId is null && !_uncertain && DateTimeOffset.UtcNow >= _subscriptionRenewAt)
                    {
                        await RequestSnapshotAsync(current.PluginId, current.Page.Id);
                    }
                    else if (_awaitingSnapshot && _navigation.Current is { } selected && DateTimeOffset.UtcNow >= _subscriptionRenewAt)
                    {
                        await RequestSnapshotAsync(selected.PluginId, selected.PageId);
                    }

                    delay = .5;
                    UpdateActionAvailability();
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (BridgeException exception) when (exception.Code == "mobile_device_revoked")
                {
                    ShowExpiredDevice();
                    break;
                }
                catch (Exception exception)
                {
                    _connected = false;
                    UpdateActionAvailability();
                    if (_pendingId is not null)
                    {
                        SetActionFeedback(T("连接中断，操作已保存，正在重连…", "Connection lost. Action saved; reconnecting…"));
                    }

                    _status.Text = exception is BridgeException e ? e.Code : T("连接中断，正在重连", "Connection interrupted; reconnecting");
                    delay = Math.Min(60, delay * 2);
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(delay), token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        finally
        {
            _pollGate.Release();
        }
    }
}
