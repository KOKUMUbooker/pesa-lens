using System.Globalization;
using PesaScope.Core.Models;

namespace PesaScope.App.Converters;

// ── Visibility helpers ────────────────────────────────────────────────────────

/// <summary>Returns true when the bound int equals zero (used to show empty-state labels).</summary>
public class IntIsZeroConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int n && n == 0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Returns true when the bound int is greater than zero (used to show the list card).</summary>
public class IntIsNonZeroConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int n && n > 0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

// ── Transaction type -> display ────────────────────────────────────────────────

/// <summary>Maps a TransactionType to a simple emoji used as an icon in the transaction row.</summary>
public class TransactionTypeToEmojiConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is TransactionType t ? t switch
        {
            TransactionType.ReceiveMoney => "💚",
            TransactionType.SendMoney => "💸",
            TransactionType.PayBill => "🧾",
            TransactionType.BuyGoods => "🛒",
            TransactionType.AirtimePurchase => "📱",
            TransactionType.Withdrawal => "🏧",
            TransactionType.Deposit => "🏦",
            TransactionType.Fuliza => "⚡",
            TransactionType.MShwari => "💰",
            TransactionType.Reversal => "↩️",
            _ => "💳",
        } : "💳";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Maps a TransactionDirection to a sign prefix for the amount label.
/// Incoming -> "+"; outgoing -> "-".
/// Falls back to empty string for unknown/unset direction.
/// </summary>
public class TransactionTypeToSignConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TransactionDirection direction)
            return string.Empty;

        return direction switch
        {
            TransactionDirection.Incoming => "+",
            TransactionDirection.Outgoing => "-",
            _ => string.Empty,
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}


/// <summary>
/// Maps a TransactionDirection to a MAUI Color for the amount label.
/// Incoming -> Primary (green); outgoing -> Tertiary (red-orange).
/// Falls back to OnSurface for unknown/unset direction.
/// </summary>
public class TransactionTypeToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TransactionDirection direction)
            return GetThemeColor("OnSurface");

        return direction switch
        {
            TransactionDirection.Incoming => GetThemeColor("Primary"),
            TransactionDirection.Outgoing => GetThemeColor("Tertiary"),
            _ => GetThemeColor("OnSurface"),
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static Color GetThemeColor(string key)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var raw) == true && raw is Color c)
            return c;
        return key switch
        {
            "Primary" => Color.FromArgb("#1A8C62"),
            "Tertiary" => Color.FromArgb("#D4522A"),
            _ => Color.FromArgb("#1A2E26"),
        };
    }
}