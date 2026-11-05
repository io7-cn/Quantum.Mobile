using System.Globalization;

namespace Quantum.MobileApp.Localization;

internal static class MobileLanguage
{
    public static bool IsChinese => Preferences.Default.Get("quantum.mobile.language", CultureInfo.CurrentUICulture.Name)
        .StartsWith("zh", StringComparison.OrdinalIgnoreCase);

    public static string Text(string chinese, string english) => IsChinese ? chinese : english;
}
