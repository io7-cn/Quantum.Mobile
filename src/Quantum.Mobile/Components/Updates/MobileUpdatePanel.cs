using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using System.Globalization;

namespace Quantum.MobileApp.Components.Updates;

public sealed class MobileUpdatePanel : ContentView
{
    private readonly MobileAppUpdateService _updates;
    private readonly Label _title = new()
    {
        FontSize = 20,
        FontAttributes = FontAttributes.Bold,
        TextColor = MobileTheme.Ink
    };
    private readonly Label _version = MobileTheme.Caption("");
    private readonly Label _status = MobileTheme.Caption("");
    private readonly Label _automaticLabel = MobileTheme.Caption("");
    private readonly Label _installNote = MobileTheme.Caption("");
    private readonly Button _check = MobileTheme.PrimaryButton("");
    private readonly Button _install = MobileTheme.PrimaryButton("");
    private readonly ProgressBar _progress = new()
    {
        ProgressColor = MobileTheme.Accent
    };
    private bool _subscribed;
    private bool _installing;
    private static string T(string zh, string en) => MobileLanguage.Text(zh, en);
    public MobileUpdatePanel(MobileAppUpdateService updates)
    {
        _updates = updates;
        var automatic = new Switch
        {
            IsToggled = Preferences.Default.Get("quantum.mobile.auto-update-check", true),
            AutomationId = "mobile-update-automatic"
        };
        automatic.Toggled += async (_, e) =>
        {
            Preferences.Default.Set("quantum.mobile.auto-update-check", e.Value);
            if (e.Value && !_updates.IsBusy)
            {
                await _updates.CheckAsync();
            }
        };
        var row = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        row.Add(_automaticLabel);
        row.Add(automatic, 1);
        _check.AutomationId = "mobile-update-check";
        _install.AutomationId = "mobile-update-install";
        _check.Clicked += async (_, _) => await _updates.CheckAsync();
        _install.Clicked += async (_, _) => await InstallAsync();
        Content = MobileTheme.Card(new VerticalStackLayout { Spacing = 12, Children = { _title, _version, row, _status, _progress, _check, _install, _installNote } });
        Loaded += async (_, _) =>
        {
            if (!_subscribed)
            {
                _updates.Changed += OnChanged;
                _subscribed = true;
            }

            RefreshText();
            if (_updates.State == MobileUpdateState.Idle)
            {
                await _updates.CheckAsync();
            }
        };
        Unloaded += (_, _) =>
        {
            _updates.Changed -= OnChanged;
            _subscribed = false;
        };
        RefreshText();
    }

    private void OnChanged() => MainThread.BeginInvokeOnMainThread(RefreshText);
    public void RefreshText()
    {
        _title.Text = T("应用更新", "App updates");
        _version.Text = T("当前版本：", "Current version: ") + _updates.CurrentVersion;
        _automaticLabel.Text = T("启动时自动检查更新", "Check for updates on startup");
        _check.Text = T("检查更新", "Check for updates");
        _check.IsEnabled = !_updates.IsBusy && _updates.State != MobileUpdateState.Unavailable;
        _install.IsVisible = _updates.Target is not null && _updates.State is MobileUpdateState.Available or MobileUpdateState.Ready or MobileUpdateState.Failed;
        _install.IsEnabled = !_updates.IsBusy && !_installing;
        _install.Text = _updates.State == MobileUpdateState.Ready ? T("安装更新 / 打开更新包", "Install / open update package") : T("下载更新", "Download update");
        _progress.IsVisible = _updates.State == MobileUpdateState.Downloading;
        _progress.Progress = _updates.Progress / 100d;
        _installNote.Text = OperatingSystem.IsAndroid() ? T("安装包通过 SHA-256 校验后交给系统安装，需要你确认安装。", "Verified packages open in the system installer for your confirmation.") : T("下载并打开便携版更新包，关闭应用后替换原文件。", "Open the portable update package and replace the old files after closing the app.");
        _status.Text = _updates.State switch
        {
            MobileUpdateState.Unavailable => T("当前平台请通过应用分发渠道更新。", "Update through your app distribution channel on this platform."),
            MobileUpdateState.Checking => T("正在检查更新…", "Checking for updates…"),
            MobileUpdateState.Current => T("已是最新版本", "You are up to date"),
            MobileUpdateState.Available => T("发现新版本：", "New version: ") + _updates.Target?.Version,
            MobileUpdateState.Downloading => T("正在下载：", "Downloading: ") + _updates.Progress + "%",
            MobileUpdateState.Ready => T("更新包已下载并通过校验。", "The update package is downloaded and verified."),
            MobileUpdateState.Failed => T("更新失败，请检查网络后重试。", "Update failed. Check your connection and retry."),
            _ => T("从 Quantum 官方发布获取更新。", "Updates come from the official Quantum releases.")
        };
    }

    private async Task InstallAsync()
    {
        if (_installing)
        {
            return;
        }

        _installing = true;
        _install.IsEnabled = false;
        try
        {
            if (_updates.State != MobileUpdateState.Ready)
            {
                await _updates.DownloadAsync(Path.Combine(FileSystem.CacheDirectory, "sharing-root", "updates"));
            }

            if (_updates.State != MobileUpdateState.Ready || _updates.DownloadPath is null)
            {
                return;
            }
#if ANDROID
            var activity = Platform.CurrentActivity ?? throw new InvalidOperationException("No foreground activity.");
            if (!activity.PackageManager!.CanRequestPackageInstalls())
            {
                var intent = new Android.Content.Intent(Android.Provider.Settings.ActionManageUnknownAppSources,
                    Android.Net.Uri.Parse("package:" + activity.PackageName));
                if (intent.ResolveActivity(activity.PackageManager) is not null) activity.StartActivity(intent);
                _status.Text = T("请允许 Quantum 安装更新，返回后再次点击安装。", "Allow Quantum to install updates, then return and tap install again.");
                return;
            }
#endif
            var type = OperatingSystem.IsAndroid() ? "application/vnd.android.package-archive" : "application/zip";
            if (!await Launcher.Default.OpenAsync(new OpenFileRequest(T("Quantum 更新", "Quantum update"), new ReadOnlyFile(_updates.DownloadPath, type))))
            {
                _status.Text = T("无法打开更新包，请稍后重试。", "Could not open the update package. Please retry.");
            }
        }
        catch (Exception)
        {
            _status.Text = T("无法打开安装界面，请检查安装权限后重试。", "Could not open the installer. Check installation permissions and retry.");
        }
        finally
        {
            _installing = false;
            _install.IsEnabled = !_updates.IsBusy;
        }
    }
}
