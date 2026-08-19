using System.Globalization;
using PesaScope.Core.Models;
using AppTheme = Microsoft.Maui.ApplicationModel.AppTheme;

namespace PesaScope.App.Converters;

/// <summary>
/// Returns true when a string is non-null and non-empty.
/// Used to show/hide the search clear (✕) button.
/// </summary>
public class StringToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        !string.IsNullOrEmpty(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

/// <summary>
/// Maps a TransactionType to an emoji icon for the list row icon circle.
/// </summary>
public class TransactionTypeToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is TransactionType type ? GetIcon(type) : "💳";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();

    private static string GetIcon(TransactionType type) => type switch
    {
        TransactionType.SendMoney => "📤",
        TransactionType.ReceiveMoney => "📥",
        TransactionType.PayBill => "🧾",
        TransactionType.BuyGoods => "🛒",
        TransactionType.AirtimePurchase => "📱",
        TransactionType.Withdrawal => "🏧",
        TransactionType.Deposit => "💰",
        TransactionType.Fuliza => "⚡",
        TransactionType.MShwari => "🏦",
        TransactionType.Reversal => "↩️",
        _ => "💳"
    };
}

/// <summary>
/// Maps a TransactionType to a Color — green for incoming, red for outgoing.
/// </summary>
public class TransactionPageTypeToColorConverter : IValueConverter
{
    private static readonly Color Incoming = Color.FromArgb("#1A8C62");
    private static readonly Color Outgoing = Color.FromArgb("#C0392B");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is TransactionDirection.Outgoing ? Outgoing : Incoming;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

/// <summary>
/// Returns true if the bound value equals the ConverterParameter.
/// Used to highlight/select an item in a list when its Id matches
/// some "currently selected" value elsewhere in the ViewModel.
///
/// Usage:
/// IsVisible="{Binding SomeSelectedId, Converter={StaticResource IsEqualConverter}, ConverterParameter={Binding Id}}"
/// </summary>
public class IsEqualConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || parameter is null)
            return false;

        // Handle numeric type mismatches (e.g. int vs long) gracefully,
        // since XAML bindings can box values as different numeric types.
        if (value is IConvertible && parameter is IConvertible)
        {
            try
            {
                var valueStr = System.Convert.ToString(value, culture);
                var paramStr = System.Convert.ToString(parameter, culture);
                return string.Equals(valueStr, paramStr, StringComparison.Ordinal);
            }
            catch
            {
                // fall through to direct equality if conversion fails
            }
        }

        return value.Equals(parameter);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Compares two values (e.g. SelectedCategory.Id and the current item's Id)
/// and returns trueColor if equal, falseColor otherwise.
/// Colors are passed via ConverterParameter as "trueColorKey|falseColorKey"
/// resolved from Application resources, or pass literal Color objects directly
/// via code if binding from XAML resources isn't convenient.
/// </summary>
public class EqualsToColorMultiConverter : IMultiValueConverter
{
    public object Convert(object[]? values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is not { Length: 2 } || values[0] is null || values[1] is null)
            return Colors.Transparent;

        bool isEqual = System.Convert.ToString(values[0], culture)
            == System.Convert.ToString(values[1], culture);

        if (!isEqual)
            return Colors.Transparent;

        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var key = isDark ? "PrimaryContainerDark" : "PrimaryContainer";

        return Application.Current!.Resources.TryGetValue(key, out var color)
            ? color
            : Colors.Transparent;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class UtcToLocalDateConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime utc)
            return utc.ToLocalTime();

        return value is null 
            ? DateTime.Today.ToLocalTime()
            : value;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
