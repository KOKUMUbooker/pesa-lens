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
/// Maps a TransactionType to a sign prefix string ("+" or "-") for formatting the amount label.
/// </summary>
public class TransactionTypeToSignConverter : IValueConverter
{
    private static readonly HashSet<TransactionType> _incoming = new()
    {
        TransactionType.ReceiveMoney,
        TransactionType.Deposit,
        TransactionType.Reversal,
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is TransactionType t && _incoming.Contains(t) ? "+" : "-";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
