using System.Windows.Input;

namespace PesaScope.App.Controls.Home;

public partial class BalanceCardView : ContentView
{
    public static readonly BindableProperty NetBalanceProperty =
        BindableProperty.Create(nameof(NetBalance), typeof(string), typeof(BalanceCardView), "KSh 0");

    public static readonly BindableProperty PeriodLabelProperty =
        BindableProperty.Create(nameof(PeriodLabel), typeof(string), typeof(BalanceCardView), string.Empty);

    public static readonly BindableProperty MoneyInProperty =
        BindableProperty.Create(nameof(MoneyIn), typeof(string), typeof(BalanceCardView), "KSh 0");

    public static readonly BindableProperty MoneyOutProperty =
        BindableProperty.Create(nameof(MoneyOut), typeof(string), typeof(BalanceCardView), "KSh 0");

    public static readonly BindableProperty ShowHelpCommandProperty =
        BindableProperty.Create(nameof(ShowHelpCommand), typeof(ICommand), typeof(BalanceCardView));

    public string NetBalance
    {
        get => (string)GetValue(NetBalanceProperty);
        set => SetValue(NetBalanceProperty, value);
    }

    public string PeriodLabel
    {
        get => (string)GetValue(PeriodLabelProperty);
        set => SetValue(PeriodLabelProperty, value);
    }

    public string MoneyIn
    {
        get => (string)GetValue(MoneyInProperty);
        set => SetValue(MoneyInProperty, value);
    }

    public string MoneyOut
    {
        get => (string)GetValue(MoneyOutProperty);
        set => SetValue(MoneyOutProperty, value);
    }

    public ICommand ShowHelpCommand
    {
        get => (ICommand)GetValue(ShowHelpCommandProperty);
        set => SetValue(ShowHelpCommandProperty, value);
    }

    public BalanceCardView()
    {
        InitializeComponent();
    }
}