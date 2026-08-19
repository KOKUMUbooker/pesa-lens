using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using PesaScope.App.Data.Repositories.Interfaces;
using PesaScope.App.Views.Transactions;
using PesaScope.Core.Models;
using SkiaSharp;

namespace PesaScope.App.ViewModels;

public enum DashboardViewMode
{
    Weekly = 0,
    Monthly = 1,
    Yearly = 2
}

public enum ChartDisplayMode
{
    Graph = 0,
    Details = 1
}

public partial class WeekOption : ObservableObject
{
    public DateTime Start { get; init; }
    public DateTime End { get; init; } // inclusive, already clipped to the month
    public string Label { get; init; } = string.Empty;

    [ObservableProperty] private bool _isSelected;
}

public partial class ChartDetailItem
{
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string FormattedAmount => $"Ksh {Amount:N0}";
}

public partial class CategorySpendItem
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string FormattedAmount => $"Ksh {Amount:N0}";
}

public partial class DashboardViewModel : ObservableObject
{
    // ── Dependencies ─────────────────────────────────────────────────────────
    private readonly ITransactionRepository _transactions;
    private readonly ICategoryRepository _categories;

    // Guards against partial-property-changed hooks firing while we're
    // programmatically initializing state (e.g. when switching view modes).
    private bool _isInitializingSelection;

    // ── View mode ──────────────────────────────────────────────────────────
    [ObservableProperty] private DashboardViewMode _viewMode = DashboardViewMode.Weekly;
    [ObservableProperty] private bool _isWeeklyMode = true;
    [ObservableProperty] private bool _isMonthlyMode = false;
    [ObservableProperty] private bool _isYearlyMode = false;

    // ── Monthly mode: year / month / week selection ──────────────────────────
    [ObservableProperty] private int _selectedYear;
    [ObservableProperty] private MonthOption _selectedMonth = null!;
    [ObservableProperty] private List<WeekOption> _availableWeeks = [];
    [ObservableProperty] private WeekOption? _selectedWeek;

    public List<int> AvailableYears { get; private set; } = [];
    public List<MonthOption> AvailableMonths { get; } =
        Enumerable.Range(1, 12)
            .Select(m => new MonthOption
            {
                Value = m,
                Name = new DateTime(2000, m, 1).ToString("MMMM")
            })
            .ToList();

    // ── Period label + chart title (both vary by mode) ────────────────────────
    [ObservableProperty] private string _periodLabel = string.Empty;
    [ObservableProperty] private string _chartTitle = "Weekly spending";

    // ── Summary ───────────────────────────────────────────────────────────────
    [ObservableProperty] private decimal _moneyIn;
    [ObservableProperty] private decimal _moneyOut;
    [ObservableProperty] private decimal _netBalance;
    [ObservableProperty] private string _formattedMoneyIn = "Ksh 0";
    [ObservableProperty] private string _formattedMoneyOut = "Ksh 0";
    [ObservableProperty] private string _formattedNetBalance = "Ksh 0";

    // ── Chart ─────────────────────────────────────────────────────────────────
    [ObservableProperty] private ISeries[] _spendingSeries = [];
    [ObservableProperty] private Axis[] _xAxes = [];
    [ObservableProperty] private Axis[] _yAxes = [];

    // ── Top categories ────────────────────────────────────────────────────────
    [ObservableProperty] private List<CategorySpendItem> _topCategories = [];

    // ── Recent transactions ───────────────────────────────────────────────────
    [ObservableProperty] private List<Transaction> _recentTransactions = [];

    // ── State ─────────────────────────────────────────────────────────────────
    [ObservableProperty] private bool _isBusy = false;
    [ObservableProperty] private bool _isRefreshing = false;
    [ObservableProperty] private string _lastSynced = "Never";

    // ── Net balance help sheet ────────────────────────────────────────────────
    [ObservableProperty] private bool _isNetBalanceHelpOpen;

    public bool IsNetBalanceNegative => NetBalance < 0;

    [RelayCommand]
    private void ShowNetBalanceHelp() => IsNetBalanceHelpOpen = true;

    [RelayCommand]
    private void CloseNetBalanceHelp() => IsNetBalanceHelpOpen = false;

    [ObservableProperty] private ChartDisplayMode _chartDisplayMode = ChartDisplayMode.Graph;
    [ObservableProperty] private List<ChartDetailItem> _chartDetailItems = [];

    public bool IsChartGraphMode => ChartDisplayMode == ChartDisplayMode.Graph;
    public bool IsChartDetailsMode => ChartDisplayMode == ChartDisplayMode.Details;

    partial void OnChartDisplayModeChanged(ChartDisplayMode value)
    {
        OnPropertyChanged(nameof(IsChartGraphMode));
        OnPropertyChanged(nameof(IsChartDetailsMode));
    }

    [RelayCommand]
    private void SetChartDisplayMode(ChartDisplayMode mode) => ChartDisplayMode = mode;

    // ── Constructor ───────────────────────────────────────────────────────────
    public DashboardViewModel(ITransactionRepository transactions, ICategoryRepository categories)
    {
        _transactions = transactions;
        _categories = categories;

        var today = DateTime.Today;
        AvailableYears = Enumerable.Range(today.Year - 4, 5).Reverse().ToList();

        _isInitializingSelection = true;
        _selectedYear = today.Year;
        _selectedMonth = AvailableMonths.First(m => m.Value == today.Month);
        _availableWeeks = GenerateWeeksForMonth(today.Year, today.Month);
        SetSelectedWeek(_availableWeeks.FirstOrDefault(w => today >= w.Start && today <= w.End)
            ?? _availableWeeks.FirstOrDefault());
        _isInitializingSelection = false;
    }

    // ── View mode switching ────────────────────────────────────────────────────

    [RelayCommand]
    private async Task SwitchViewModeAsync(DashboardViewMode mode)
    {
        if (ViewMode == mode || IsBusy) return;

        ViewMode = mode;
        IsWeeklyMode = mode == DashboardViewMode.Weekly;
        IsMonthlyMode = mode == DashboardViewMode.Monthly;
        IsYearlyMode = mode == DashboardViewMode.Yearly;

        if (mode == DashboardViewMode.Monthly)
        {
            _isInitializingSelection = true;
            AvailableWeeks = GenerateWeeksForMonth(SelectedYear, SelectedMonth.Value);
            var today = DateTime.Today;
            SetSelectedWeek(AvailableWeeks.FirstOrDefault(w => today >= w.Start && today <= w.End)
                ?? AvailableWeeks.FirstOrDefault());
            _isInitializingSelection = false;
        }

        await LoadAsync();
    }

    partial void OnSelectedYearChanged(int value)
    {
        if (_isInitializingSelection) return;

        if (ViewMode == DashboardViewMode.Monthly)
            RegenerateWeeksAndReload();
        else if (ViewMode == DashboardViewMode.Yearly)
            _ = LoadAsync();
    }

    partial void OnSelectedMonthChanged(MonthOption value)
    {
        if (_isInitializingSelection) return;
        if (ViewMode == DashboardViewMode.Monthly)
            RegenerateWeeksAndReload();
    }

    private void RegenerateWeeksAndReload()
    {
        _isInitializingSelection = true;
        AvailableWeeks = GenerateWeeksForMonth(SelectedYear, SelectedMonth.Value);
        var today = DateTime.Today;
        SetSelectedWeek(AvailableWeeks.FirstOrDefault(w => today >= w.Start && today <= w.End)
            ?? AvailableWeeks.FirstOrDefault());
        _isInitializingSelection = false;

        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task SelectWeekAsync(WeekOption week)
    {
        if (SelectedWeek == week) return;
        SetSelectedWeek(week);
        await LoadAsync();
    }

    // ── Week generation (clipped to month boundaries) ─────────────────────────

    private static List<WeekOption> GenerateWeeksForMonth(int year, int month)
    {
        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var weeks = new List<WeekOption>();

        // Monday on/before monthStart — the calendar week containing the 1st.
        int daysSinceMonday = ((int)monthStart.DayOfWeek + 6) % 7;
        var cursor = monthStart.AddDays(-daysSinceMonday);

        while (cursor <= monthEnd)
        {
            var weekEnd = cursor.AddDays(6);
            var clippedStart = cursor < monthStart ? monthStart : cursor;
            var clippedEnd = weekEnd > monthEnd ? monthEnd : weekEnd;

            weeks.Add(new WeekOption
            {
                Start = clippedStart,
                End = clippedEnd,
                Label = clippedStart.Date == clippedEnd.Date
                    ? clippedStart.ToString("d MMM")
                    : $"{clippedStart:d MMM} – {clippedEnd:d MMM}"
            });

            cursor = cursor.AddDays(7);
        }

        return weeks;
    }

    // ── Resolved period for the active view mode ──────────────────────────────

    private (DateTime from, DateTime to) ResolveCurrentPeriod()
    {
        switch (ViewMode)
        {
            case DashboardViewMode.Monthly when SelectedWeek is not null:
                return (SelectedWeek.Start, SelectedWeek.End.AddHours(23).AddMinutes(59).AddSeconds(59));

            case DashboardViewMode.Yearly:
                var yearStart = new DateTime(SelectedYear, 1, 1);
                var yearEnd = SelectedYear == DateTime.Today.Year
                    ? DateTime.Today
                    : new DateTime(SelectedYear, 12, 31);
                return (yearStart, yearEnd.AddHours(23).AddMinutes(59).AddSeconds(59));

            default: // Weekly, or Monthly with no week resolved yet
                return CurrentWeekRange();
        }
    }

    private static (DateTime from, DateTime to) CurrentWeekRange()
    {
        var today = DateTime.Today;
        int daysSinceMon = ((int)today.DayOfWeek + 6) % 7;
        var weekStart = today.AddDays(-daysSinceMon);
        var weekEnd = weekStart.AddDays(6).AddHours(23).AddMinutes(59).AddSeconds(59);
        return (weekStart, weekEnd);
    }

    private string ResolvePeriodLabel((DateTime from, DateTime to) period)
    {
        return ViewMode switch
        {
            DashboardViewMode.Yearly => SelectedYear.ToString(),
            DashboardViewMode.Monthly when SelectedWeek is not null => SelectedWeek.Label,
            _ => $"{period.from:d MMM} – {period.to:d MMM yyyy}"
        };
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await RefreshDashboardAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        try
        {
            await RefreshDashboardAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private static async Task NavigateToTransactionsAsync() =>
        await Shell.Current.GoToAsync("//TransactionsPage");

    [RelayCommand]
    private static async Task OpenTransactionAsync(Transaction transaction) =>
        await Shell.Current.GoToAsync(
            $"{nameof(TransactionDetailPage)}?code={transaction.MpesaCode}");

    // ── Core refresh logic ────────────────────────────────────────────────────

    private async Task RefreshDashboardAsync()
    {
        var period = ResolveCurrentPeriod();
        PeriodLabel = ResolvePeriodLabel(period);
        ChartTitle = ViewMode == DashboardViewMode.Yearly ? "Monthly spending" : "Daily spending";

        var chartTask = ViewMode == DashboardViewMode.Yearly
            ? LoadYearlyChartAsync(SelectedYear)
            : LoadDailyChartAsync(period.from, period.to);

        await Task.WhenAll(
            LoadSummaryAsync(period.from, period.to),
            chartTask,
            LoadTopCategoriesAsync(period.from, period.to),
            LoadRecentTransactionsAsync()
        );

        ChartTitle = ViewMode switch
        {
            DashboardViewMode.Yearly => "Monthly spending",
            DashboardViewMode.Weekly => "Weekly spending",
            _ => "Daily spending"
        };

        LastSynced = $"Updated {DateTime.Now:h:mm tt}";
    }

    private async Task LoadSummaryAsync(DateTime from, DateTime to)
    {
        var inTask = _transactions.GetTotalReceivedAsync(from, to);
        var outTask = _transactions.GetTotalSpentAsync(from, to);

        await Task.WhenAll(inTask, outTask);

        MoneyIn = inTask.Result;
        MoneyOut = outTask.Result;
        NetBalance = MoneyIn - MoneyOut;

        FormattedMoneyIn = $"Ksh {MoneyIn:N0}";
        FormattedMoneyOut = $"Ksh {MoneyOut:N0}";
        FormattedNetBalance = $"Ksh {NetBalance:N0}";
    }

    // ── Daily chart (used for Weekly and Monthly modes) ───────────────────────

    private async Task LoadDailyChartAsync(DateTime from, DateTime to)
    {
        var daily = await _transactions.GetDailySpendingAsync(from, to);

        int dayCount = (to.Date - from.Date).Days + 1;
        var labels = new string[dayCount];
        var values = new double[dayCount];
        var details = new List<ChartDetailItem>(dayCount);

        for (int i = 0; i < dayCount; i++)
        {
            var day = from.Date.AddDays(i);
            var amount = daily.TryGetValue(day, out var amt) ? amt : 0m;

            labels[i] = day.ToString("ddd");
            values[i] = (double)amount;

            details.Add(new ChartDetailItem
            {
                // "Monday, 12 Aug" reads well for both weekly and monthly modes
                Label = day.ToString("dddd, d MMM"),
                Amount = amount
            });
        }

        ChartDetailItems = details;
        BuildChart(labels, values);
    }

    // ── Monthly-bucket chart (used for Yearly mode) ───────────────────────────

    private async Task LoadYearlyChartAsync(int year)
    {
        var yearStart = new DateTime(year, 1, 1);
        var lastDay = year == DateTime.Today.Year ? DateTime.Today : new DateTime(year, 12, 31);
        var yearEnd = lastDay.Date.AddDays(1).AddTicks(-1);

        var daily = await _transactions.GetDailySpendingAsync(yearStart, yearEnd);

        var monthTotals = new decimal[12];
        foreach (var (date, amount) in daily)
        {
            if (date.Year == year)
                monthTotals[date.Month - 1] += amount;
        }

        var labels = Enumerable.Range(1, 12)
            .Select(m => new DateTime(2000, m, 1).ToString("MMMM")[..1])
            .ToArray();
        var values = monthTotals.Select(v => (double)v).ToArray();

        ChartDetailItems = Enumerable.Range(1, 12)
            .Select(m => new ChartDetailItem
            {
                Label = new DateTime(2000, m, 1).ToString("MMMM"),
                Amount = monthTotals[m - 1]
            })
            .ToList();

        BuildChart(labels, values);
    }

    private void BuildChart(string[] labels, double[] values)
    {
        var gridLinePaint = new SolidColorPaint(new SKColor(0xD6, 0xE2, 0xDC, 0x99))
        {
            StrokeThickness = 0.8f
        };
        var labelPaint = new SolidColorPaint(new SKColor(0x4B, 0x5F, 0x56));

        SpendingSeries =
        [
            new ColumnSeries<double>
            {
                Values = values,
                Fill = new SolidColorPaint(SKColor.Parse("#1A8C62")),
                Stroke = null,
                MaxBarWidth = 40,
                Rx = 6,
                Ry = 6,
                Name = "Spend",
                DataLabelsPaint = null,
            }
        ];

        XAxes =
        [
            new Axis
            {
                Labels = labels,
                TextSize = 11,
                LabelsPaint = labelPaint,
                SeparatorsPaint = null,
                TicksPaint = null,
                MinStep = 1,
                ForceStepToMin = true,
            }
        ];

        YAxes =
        [
            new Axis
            {
                TextSize = 10,
                LabelsPaint = labelPaint,
                SeparatorsPaint = gridLinePaint,
                TicksPaint = null,
                Labeler = val => val switch
                    {
                        0 => "0",
                        < 1000 => $"Ksh {val:0}",
                        < 1_000_000 => $"Ksh {val / 1000:0.#}k",
                        _ => $"Ksh {val / 1_000_000:0.#}M"
                    },
            }
        ];
    }

    private async Task LoadTopCategoriesAsync(DateTime from, DateTime to)
    {
        var spendByCategory = await _transactions.GetSpendingByCategoryAsync(from, to);
        var allCategories = await _categories.GetAllAsync();

        var catMap = allCategories.ToDictionary(c => c.Id);

        var top3 = spendByCategory
            .Where(kvp => catMap.ContainsKey(kvp.Key))
            .OrderByDescending(kvp => kvp.Value)
            .Take(3)
            .Select(kvp => new CategorySpendItem
            {
                Name = catMap[kvp.Key].Name,
                Icon = catMap[kvp.Key].Icon,
                Color = catMap[kvp.Key].Color,
                Amount = kvp.Value
            })
            .ToList();

        TopCategories = top3;
    }

    partial void OnNetBalanceChanged(decimal value) => OnPropertyChanged(nameof(IsNetBalanceNegative));

    private async Task LoadRecentTransactionsAsync()
    {
        // Always the latest 5 overall, independent of the selected period.
        RecentTransactions = await _transactions.GetRecentAsync(5);
    }

    private void SetSelectedWeek(WeekOption? week)
    {
        foreach (var w in AvailableWeeks)
            w.IsSelected = false;

        if (week is not null)
            week.IsSelected = true;

        SelectedWeek = week;
    }
}

