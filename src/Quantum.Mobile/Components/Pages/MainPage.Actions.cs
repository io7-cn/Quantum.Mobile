using Quantum.Mobile.Internal;
using System.Text.Json;

namespace Quantum.MobileApp.Components.Pages;

public sealed partial class MainPage
{
    private async Task ConfirmPendingAsync()
    {
        if (_uncertain && await DisplayAlertAsync(T("确认业务结果", "Confirm the outcome"), T("请先在桌面检查操作是否已经生效。", "Check whether the operation already took effect on desktop."), T("已核对", "Checked"), T("取消", "Cancel")))
        {
            ClearPending();
            _uncertain = false;
            _notice.IsVisible = false;
            if (_snapshot is { } snapshot)
            {
                await RequestSnapshotAsync(snapshot.PluginId, snapshot.Page.Id);
            }
        }
    }

    private async Task InvokeAsync(string actionId, Dictionary<string, JsonElement> inputs)
    {
        if (_preview)
        {
            _status.Text = "MOCK · " + JsonSerializer.Serialize(inputs, MobileJson.TypeInfo<Dictionary<string, JsonElement>>());
            if (_pendingId is not null)
            {
                return;
            }

            _pendingId = Guid.NewGuid().ToString("N");
            _outbox = new(new BridgeSend(_pendingId, "mock", "action", MobileJson.Element(new JsonObject())), "mock", _snapshot!.PluginId, _snapshot.Page.Id, _snapshot.ExpiresAt, inputs);
            _renderer?.ResetSubmittedInputs(inputs);
            SetActionFeedback(T("待发送…", "Queued…"));
            UpdateActionAvailability();
            return;
        }

        if (_savingAction || _uncertain || _pendingId is not null || _awaitingSnapshot || _snapshot is null || _snapshot.ExpiresAt <= _client!.UtcNow)
        {
            ShowNotice(T("请先刷新页面或确认之前的结果", "Refresh the page or check the previous result first"));
            return;
        }

        var id = Guid.NewGuid().ToString("N");
        var pending = new PendingAction(new BridgeSend(id, _desktopId!, "action", MobileJson.Element(new BridgeInvoke(_snapshot.ViewId, actionId, inputs))), _client.Registration!.Device.Id, _snapshot.PluginId, _snapshot.Page.Id, _snapshot.ExpiresAt, inputs);
        _savingAction = true;
        SetActionFeedback(T("正在保存…", "Saving…"));
        UpdateActionAvailability();
        try
        {
            // Persist the complete immutable command before acknowledging the tap or touching inputs.
            await SecureStorage.Default.SetAsync(_storageKey + ".outbox", JsonSerializer.Serialize(pending, ClientJsonContext.Default.PendingAction));
            _outbox = pending;
            _pendingId = id;
            _renderer?.ResetSubmittedInputs(inputs);
            SetActionFeedback(T("待发送…", "Queued…"));
            UpdateActionAvailability();
            _ = FlushOutboxAsync(CancellationToken.None);
        }
        catch (Exception)
        {
            ShowNotice(T("无法保存操作，请重试", "Could not save the action. Please try again."));
        }
        finally
        {
            _savingAction = false;
            UpdateActionAvailability();
        }
    }

    private void ClearPending()
    {
        _pendingId = null;
        _outbox = null;
        if (!_preview)
        {
            Preferences.Default.Remove(_storageKey + ".pending");
            SecureStorage.Default.Remove(_storageKey + ".outbox");
        }
    }

    private void Outcome(BridgeOutcome outcome)
    {
        if (_pendingId != outcome.RequestId)
        {
            return;
        }

        _status.Text = MobileFeedback.Outcome(outcome, Chinese);
        SetActionFeedback(_status.Text ?? "");
        if (outcome.Status is "accepted" or "running")
        {
            return;
        }

        if (_lastOutcomeKey == outcome.RequestId + outcome.Status)
        {
            return;
        }

        _lastOutcomeKey = outcome.RequestId + outcome.Status;
        _uncertain = outcome.Status is not ("succeeded" or "failed");
        if (outcome.Status == "failed")
        {
            RetainFailedDraft();
        }

        if (outcome.Status == "succeeded" && _outbox is { } completed && _failedDraft is { } failed && completed.PluginId == failed.PluginId && completed.PageId == failed.PageId)
        {
            _failedDraft = null;
            _offerFailedDraft = false;
            if (_restoreDraft is not null)
            {
                _restoreDraft.IsVisible = false;
            }

            if (!_preview)
            {
                SecureStorage.Default.Remove(_storageKey + ".failed-draft");
            }
        }

        if (!_uncertain)
        {
            ClearPending();
        }

        UpdateActionAvailability();
        if (_uncertain || outcome.Status == "failed")
        {
            ShowNotice(_status.Text ?? "");
        }

        if (!_uncertain && _deferredSnapshot is { } next)
        {
            _deferredSnapshot = null;
            ShowSnapshot(next);
        }
        else if (!_uncertain && _snapshot is { } snapshot)
        {
            _subscriptionRenewAt = DateTimeOffset.MinValue;
        }
    }
}
