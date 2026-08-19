using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using PesaScope.App.ViewModels;
using System.Windows.Input;

namespace PesaScope.App.Controls.Home;

public partial class SpendingChartView : ContentView
{
    public static readonly BindableProperty ChartTitleProperty =
        BindableProperty.Create(nameof(ChartTitle), typeof(string), typeof(SpendingChartView), "Weekly spending");

    public static readonly BindableProperty PeriodLabelProperty =
        BindableProperty.Create(nameof(PeriodLabel), typeof(string), typeof(SpendingChartView), string.Empty);

    public static readonly BindableProperty SeriesProperty =
        BindableProperty.Create(nameof(Series), typeof(ISeries[]), typeof(SpendingChartView), Array.Empty<ISeries>());

    public static readonly BindableProperty XAxesProperty =
        BindableProperty.Create(nameof(XAxes), typeof(Axis[]), typeof(SpendingChartView), Array.Empty<Axis>());

    public static readonly BindableProperty YAxesProperty =
        BindableProperty.Create(nameof(YAxes), typeof(Axis[]), typeof(SpendingChartView), Array.Empty<Axis>());

    public static readonly BindableProperty IsGraphModeProperty =
        BindableProperty.Create(nameof(IsGraphMode), typeof(bool), typeof(SpendingChartView), true);

    public static readonly BindableProperty DetailItemsProperty =
        BindableProperty.Create(nameof(DetailItems), typeof(List<ChartDetailItem>), typeof(SpendingChartView),
            new List<ChartDetailItem>());

    public static readonly BindableProperty SetDisplayModeCommandProperty =
        BindableProperty.Create(nameof(SetDisplayModeCommand), typeof(ICommand), typeof(SpendingChartView));

    public string ChartTitle
    {
        get => (string)GetValue(ChartTitleProperty);
        set => SetValue(ChartTitleProperty, value);
    }

    public string PeriodLabel
    {
        get => (string)GetValue(PeriodLabelProperty);
        set => SetValue(PeriodLabelProperty, value);
    }

    public ISeries[] Series
    {
        get => (ISeries[])GetValue(SeriesProperty);
        set => SetValue(SeriesProperty, value);
    }

    public Axis[] XAxes
    {
        get => (Axis[])GetValue(XAxesProperty);
        set => SetValue(XAxesProperty, value);
    }

    public Axis[] YAxes
    {
        get => (Axis[])GetValue(YAxesProperty);
        set => SetValue(YAxesProperty, value);
    }

    public bool IsGraphMode
    {
        get => (bool)GetValue(IsGraphModeProperty);
        set => SetValue(IsGraphModeProperty, value);
    }

    public List<ChartDetailItem> DetailItems
    {
        get => (List<ChartDetailItem>)GetValue(DetailItemsProperty);
        set => SetValue(DetailItemsProperty, value);
    }

    public ICommand SetDisplayModeCommand
    {
        get => (ICommand)GetValue(SetDisplayModeCommandProperty);
        set => SetValue(SetDisplayModeCommandProperty, value);
    }

    public SpendingChartView()
    {
        InitializeComponent();
    }
}