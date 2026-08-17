using PesaScope.App.ViewModels;
using System.Globalization;

namespace PesaScope.App.Converters;

// ─────────────────────────────────────────────────────────────────────────────
// Already defined in DashboardConverters.cs — kept here for completeness.
// If you keep both files, delete the duplicate classes from whichever file
// you prefer and leave only one definition per class in the project.
// ─────────────────────────────────────────────────────────────────────────────

// ── Boolean helpers ───────────────────────────────────────────────────────────

/// <summary>
/// Flips a bool.  Useful for IsVisible="{Binding Flag, Converter={StaticResource InverseBool}}".
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b && !b;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b && !b;
}

// ── String helpers ────────────────────────────────────────────────────────────

/// <summary>
/// Returns true when the string is non-null and non-empty.
/// Use IsVisible="{Binding ErrorMessage, Converter={StaticResource StringIsNotEmptyConverter}}".
/// </summary>
public class StringIsNotEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string s && s.Length > 0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Picks one of two pipe-separated strings based on a bool value.
/// ConverterParameter="TrueText|FalseText"
/// e.g. Converter={StaticResource BoolToStringConverter}, ConverterParameter="Edit|Set budget"
/// </summary>
public class BoolToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var parts = (parameter?.ToString() ?? "|").Split('|');
        var trueText = parts.Length > 0 ? parts[0] : string.Empty;
        var falseText = parts.Length > 1 ? parts[1] : string.Empty;
        return value is bool b && b ? trueText : falseText;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

// ── Colour helpers ────────────────────────────────────────────────────────────

/// <summary>
/// Converts a CSS hex string ("#1A8C62" or "1A8C62") to a MAUI <see cref="Color"/>.
/// Falls back to Transparent on any parse failure.
/// </summary>
public class HexToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string hex || string.IsNullOrWhiteSpace(hex))
            return Colors.Transparent;

        try { return Color.FromArgb(hex.StartsWith('#') ? hex : $"#{hex}"); }
        catch { return Colors.Transparent; }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

// <summary>
// Maps a <see cref="BudgetRowItem.StatusKey"/> string to a MAUI Color,
// reading live values from the application's resource dictionary so that
// light/dark theming is respected automatically.
//
// StatusKey values:
//   "over"  -> Error   (red)
//   "warn"  -> Secondary (amber)
//   "ok"    -> Primary (green)
// </summary>
public class BudgetStatusToColorConverter : IValueConverter
{
    // Resource key pairs: [light-key, dark-key]
    private static readonly Dictionary<string, (string Light, string Dark)> _map = new()
    {
        { "over", ("Error",     "ErrorDark")     },
        { "warn", ("Secondary", "SecondaryDark") },
        { "ok",   ("Primary",   "PrimaryDark")   },
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = value as string ?? "ok";
        if (!_map.TryGetValue(key, out var pair))
            pair = ("Primary", "PrimaryDark");

        return GetThemeColor(pair.Light, pair.Dark);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static Color GetThemeColor(string lightKey, string darkKey)
    {
        var resources = Application.Current?.Resources;
        if (resources is null) return Colors.Gray;

        // Prefer the key that matches the current app theme
        bool isDark = Application.Current?.RequestedTheme == Microsoft.Maui.ApplicationModel.AppTheme.Dark;
        var preferredKey = isDark ? darkKey : lightKey;
        var fallbackKey = isDark ? lightKey : darkKey;

        if (resources.TryGetValue(preferredKey, out var preferred) && preferred is Color c1)
            return c1;

        if (resources.TryGetValue(fallbackKey, out var fallback) && fallback is Color c2)
            return c2;

        // Hard-coded fallbacks matching the theme tokens in Colors.xaml
        return lightKey switch
        {
            "Error" => Color.FromArgb("#C0392B"),
            "Secondary" => Color.FromArgb("#C98A00"),
            _ => Color.FromArgb("#1A8C62"),
        };
    }
}

// ── String equality helpers (used by EditCategoryPage colour swatch) ──────────

/// <summary>
/// Returns true when the bound string equals the ConverterParameter string.
/// Used to highlight the selected colour swatch in the grid.
///
/// Usage:
///   IsVisible="{Binding ., Converter={StaticResource StrEqBool},
///              ConverterParameter='#1A8C62'}"
///
/// Note: In XAML the ConverterParameter is a literal string, so this works
/// for the palette swatch where we compare each item to a known hex value.
/// For a dynamic SelectedColor comparison, use the DataTrigger approach shown
/// in EditCategoryPage.xaml instead.
/// </summary>
public class StringEqualsBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.OrdinalIgnoreCase);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Alias kept for compatibility with any XAML already using x:Key="StrEq".
/// </summary>
public class StringEqualsConverter : StringEqualsBoolConverter { }
