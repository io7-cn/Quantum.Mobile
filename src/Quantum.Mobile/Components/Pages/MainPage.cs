using Quantum.Mobile.Internal;
using System.Globalization;

namespace Quantum.MobileApp.Components.Pages;

public sealed partial class MainPage : ContentPage
{
    private readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(12)
    };
    private string _baseUrl = Preferences.Default.Get("quantum.mobile.platform", PlatformAddress.DefaultUrl);
    private Entry _email = new()
    {
        Keyboard = Keyboard.Email
    };
    private Entry _password = new()
    {
        IsPassword = true
    };
    private readonly Label _status = new()
    {
        FontSize = 13
    };
    private readonly Label _desktopStatus = new()
    {
        FontSize = 12
    };
    private readonly VerticalStackLayout _body = new()
    {
        Spacing = 16
    };
    private readonly VerticalStackLayout _pageContent = new()
    {
        Spacing = 16
    };
    private readonly VerticalStackLayout _notice = new()
    {
        Spacing = 8,
        IsVisible = false
    };
    private readonly RefreshView _viewport = new();
    private readonly Dictionary<string, BridgeSnapshot> _pageCache = new();
    private readonly View _signInHeader = null!;
    private DateTimeOffset _subscriptionRenewAt;
    private bool _awaitingSnapshot;
    private readonly SemaphoreSlim _pollGate = new(1);
    private readonly SemaphoreSlim _resumeGate = new(1);
    private bool _resumeRequested;
    private BridgeClient? _client;
    private PlatformSession? _authentication;
    private string _storageKey = "";
    private string? _desktopId;
    private string? _pendingId;
    private BridgeSnapshot? _snapshot;
    private MobileRenderer? _renderer;
    private CancellationTokenSource? _polling;
    private Task? _pollTask;
    private bool _preview;
    private bool _connected;
    private bool _uncertain;
    private string? _openRequestId;
    private string? _lastOutcomeKey;
    private static string T(string zh, string en) => MobileLanguage.Text(zh, en);
    private static bool Chinese => MobileLanguage.IsChinese;

    public MainPage()
    {
        ApplyLanguage(Preferences.Default.Get("quantum.mobile.language", CultureInfo.CurrentUICulture.TwoLetterISOLanguageName));
        Title = "Quantum";
        BackgroundColor = MobileTheme.Canvas;
        SafeAreaEdges = Microsoft.Maui.SafeAreaEdges.All;
        NavigationPage.SetHasNavigationBar(this, false);
        var root = new VerticalStackLayout
        {
            Padding = new Thickness(18, 12),
            Spacing = 16,
            MaximumWidthRequest = 480,
            HorizontalOptions = LayoutOptions.Fill
        };
        _status.TextColor = MobileTheme.Muted;
        _desktopStatus.TextColor = MobileTheme.Muted;
        _notice.Children.Add(_noticeText);
        _reviewPending = Button(T("核对桌面后清除待确认状态", "Clear pending state after checking desktop"), ConfirmPendingAsync);
        _dismissNotice = Button(T("关闭提示", "Dismiss"), () =>
        {
            _notice.IsVisible = false;
            return Task.CompletedTask;
        });
        _notice.Children.Add(_reviewPending);
        _notice.Children.Add(_dismissNotice);
        root.Children.Add(_actionFeedback);
        root.Children.Add(_notice);
        root.Children.Add(_body);
        _signInHeader = CreateHeader();
        _viewport.Content = new ScrollView
        {
            Content = root
        };
        _viewport.RefreshColor = MobileTheme.Accent;
        _viewport.Refreshing += async (_, _) => await RefreshFromPullAsync();
        var shell = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            }
        };
        shell.Add(_viewport, 0, 1);
        BuildNavigation(shell);
        BuildAccountMenu(shell);
        Content = shell;
        ShowSignIn();
    }

    private Button Button(string text, Func<Task> action)
    {
        var button = MobileTheme.PrimaryButton(text);
        button.Clicked += async (_, _) =>
        {
            button.IsEnabled = false;
            try
            {
                await action();
            }
            catch (Exception exception)
            {
                if (_isSignIn)
                {
                    _signInError.Text = SignInError(exception);
                    _signInError.IsVisible = true;
                }
                else
                {
                    _status.Text = FriendlyError(exception);
                }
            }
            finally
            {
                button.IsEnabled = true;
            }
        };
        return button;
    }
}
