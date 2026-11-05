namespace Quantum.MobileApp.Components.Pages;

public sealed partial class MainPage
{
    private readonly MobileAppUpdateService _appUpdates = new(new HttpClient { Timeout = TimeSpan.FromMinutes(10) }, AppInfo.Current.VersionString, long.TryParse(AppInfo.Current.BuildString, out var build) ? build : 0, OperatingSystem.IsAndroid() ? "android" : OperatingSystem.IsWindows() ? "win-x64" : null);
    private bool _updateStartupChecked;
    private async Task CheckUpdatesOnStartupAsync()
    {
        if (_updateStartupChecked || !Preferences.Default.Get("quantum.mobile.auto-update-check", true))
        {
            return;
        }

        _updateStartupChecked = true;
        await _appUpdates.CheckAsync();
        await MainThread.InvokeOnMainThreadAsync(RenderAccountMenu);
    }
}
