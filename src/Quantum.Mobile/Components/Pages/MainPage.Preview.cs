using Quantum.Mobile.Internal;
using System.Text.Json;
using MobileUi = Quantum.Mobile.Internal.Mobile;

namespace Quantum.MobileApp.Components.Pages;

public sealed partial class MainPage
{
#if DEBUG
    private void Preview()

    {

        _preview = true;
        _status.Text = T("模拟器 · MOCK · 不执行业务", "Simulator · MOCK · No business execution");

        var page = MobileUi.Page("preview", "Quick capture", MobileUi.VStack("root",

            MobileUi.Text("title", "A thought worth keeping.").Appearance("title"),

            MobileUi.Text("subtitle", "Write it here. Your desktop takes care of the rest.").Appearance("caption"),

            MobileUi.Divider("line"), MobileUi.TextField("content").Hint("What's on your mind?").Limit(500),

            MobileUi.Toggle("pinned", "Pin this note"),
            new MobileNode("Attachments", "attachments"),

            new MobileNode("Button", "save") { Text = "Save to desktop", ActionId = "mock-save", InputIds = ["content", "pinned", "attachments"] })) with
        { SchemaVersion = 2 };

        RenderPreview(page);
        SetupPreviewNavigation(page);

    }

    private void RenderPreview(MobilePage page)

    {
        _isSignIn = false;
        _status.IsVisible = true;

        _preview = true;
        _snapshot = null;

        _status.Text = T("模拟器 · MOCK · 不执行业务", "Simulator · MOCK · No business execution");

        ShowSnapshot(new("preview", "mock", page.Id == "mock-status" ? "mock.status" : "mock", "mock", DateTimeOffset.UtcNow.AddMinutes(5), page));
        _status.Text = T("模拟器 · MOCK · 不执行业务", "Simulator · MOCK · No business execution");

        _pageContent.IsEnabled = true;

        // Preview is deliberately isolated from platform transport.

        _body.Children.Add(Button(T("退出模拟器", "Close simulator"), () => { _preview = false; _snapshot = null; ResetNavigation(); ShowSignIn(); return Task.CompletedTask; }));

    }

#if WINDOWS || MACCATALYST

    private async Task PreviewFileAsync()

    {

        var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Quantum mobile UI JSON" });

        if (file is null)
        {
            return;
        }

        await using var stream = await file.OpenReadAsync();
        using var memory = new MemoryStream();

        var buffer = new byte[4096];

        int count;

        while ((count = await stream.ReadAsync(buffer)) > 0)

        {

            if (memory.Length + count > MobileUiValidator.MaximumPageBytes)
            {
                throw new ArgumentException("UI JSON exceeds 128 KiB.");
            }

            memory.Write(buffer, 0, count);

        }

        var page = JsonSerializer.Deserialize<MobilePage>(memory.ToArray(), MobileJson.Options)!;

        MobileUiValidator.Validate(page);

        MobileNode mock(MobileNode node) => node with
        {
            Action = null,

            ActionId = node.Action is null ? null : "mock-" + node.Id,

            InputIds = node.Action?.Inputs.Values.Distinct().ToArray() ?? [],
            Children = node.Children.Select(mock).ToArray()
        };

        RenderPreview(page with { Root = mock(page.Root) });

    }

#endif

#endif
}
