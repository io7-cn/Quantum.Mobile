namespace Quantum.MobileApp.Components.Pages;

public sealed partial class MainPage
{
    private ContentPage? _settingsPage;
    private async Task ShowSettingsAsync()
    {
        await CloseNavigationAsync();
        await CloseMenuAsync();
        var page = _settingsPage = new ContentPage
        {
            Title = T("设置", "Settings"),
            BackgroundColor = MobileTheme.Canvas,
            SafeAreaEdges = Microsoft.Maui.SafeAreaEdges.All
        };
        var updates = new MobileUpdatePanel(_appUpdates);
        var language = new Picker
        {
            Title = T("语言", "Language"),
            ItemsSource = new[]
            {
                "简体中文",
                "English"
            },
            SelectedIndex = Chinese ? 0 : 1
        };
        language.SelectedIndexChanged += (_, _) =>
        {
            ApplyLanguage(language.SelectedIndex == 0 ? "zh-CN" : "en-US");
            page.Title = T("设置", "Settings");
            updates.RefreshText();
            RenderNavigation();
            RenderAccountMenu();
        };
        var stack = new VerticalStackLayout
        {
            Padding = 24,
            Spacing = 16,
            Children =
            {
                language,
                updates,
                Button(T("退出登录", "Sign out"), async () =>
                {
                    await Navigation.PopAsync();
                    await SignOutAsync();
                })
            }
        };
        page.Content = new ScrollView
        {
            Content = stack
        };
        await Navigation.PushAsync(page);
    }
}
