using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PesaScope.App.Data.Repositories.Interfaces;
using PesaScope.Core.Models;
using UraniumUI.Icons.MaterialSymbols;

namespace PesaScope.App.ViewModels;

/// <summary>
/// Represents a single category row in the history view.
/// Mirrors BudgetRow but is derived from a snapshot rather than live data.
/// </summary>
public class BudgetHistoryRow
{
    public string CategoryName { get; init; } = string.Empty;
    public decimal Spent { get; init; }
    public decimal Limit { get; init; }
    public bool HasLimit => Limit > 0;
    public bool WasExceeded { get; init; }

    public double Progress => HasLimit ? Math.Min((double)(Spent / Limit), 1.0) : 0;

    public string FormattedSpent => $"Ksh {Spent:N0}";
    public string FormattedLimit => HasLimit ? $"Ksh {Limit:N0}" : "No limit";

    public bool IsWarning => HasLimit && !WasExceeded &&
                             (double)(Spent / Limit) * 100 >= 80;

    public Color ProgressColor => WasExceeded
        ? Color.FromArgb("#C0392B")
        : IsWarning
            ? Color.FromArgb("#C98A00")
            : Color.FromArgb("#1A8C62");

    public string StatusLabel => WasExceeded
        ? $"Exceeded by Ksh {Spent - Limit:N0}"
        : HasLimit
            ? $"Ksh {Limit - Spent:N0} under limit"
            : $"Ksh {Spent:N0} spent";

    // Tinted background for the status pill, derived from the same status color
    public Color StatusBackgroundColor => ProgressColor.WithAlpha(0.14f);

    public string StatusEmoji => WasExceeded ? "🔴" : IsWarning ? "🟡" : "✅";

    public string StatusIcon => WasExceeded
        ? MaterialSharp.Cancel
        : IsWarning
            ? MaterialSharp.Error
            : MaterialSharp.Check_circle;
}

public partial class BudgetHistoryViewModel : ObservableObject
{
    private readonly IBudgetSnapshotRepository _snapshotRepo;

    // ── Picker state ──────────────────────────────────────────────────────────
    [ObservableProperty] private List<int> _availableYears = [];
    [ObservableProperty] private List<string> _availableMonths = [];
    [ObservableProperty] private int _selectedYear;
    [ObservableProperty] private string _selectedMonthName = string.Empty;

    // ── Derived month index (1–12) resolved from SelectedMonthName ────────────
    private int SelectedMonthIndex =>
        _availableMonths.IndexOf(SelectedMonthName) + 1;

    // ── Content ───────────────────────────────────────────────────────────────
    [ObservableProperty] private BudgetHistoryRow? _overallRow;
    [ObservableProperty] private List<BudgetHistoryRow> _categoryRows = [];
    [ObservableProperty] private bool _hasOverallLimit;

    // ── Load states ───────────────────────────────────────────────────────────
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasData;
    [ObservableProperty] private bool _isPickerReady;

    public BudgetHistoryViewModel(IBudgetSnapshotRepository snapshotRepo)
    {
        _snapshotRepo = snapshotRepo;
    }

    // ── Load ──────────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        IsPickerReady = false;

        try
        {
            var years = await _snapshotRepo.GetAvailableYearsAsync();

            if (years.Count == 0)
            {
                // No snapshots yet — nothing to show
                HasData = false;
                IsPickerReady = false;
                return;
            }

            AvailableYears = years;

            // Build month names (always all 12 — filter by availability via HasData)
            AvailableMonths = Enumerable.Range(1, 12)
                .Select(m => new DateTime(2000, m, 1).ToString("MMMM"))
                .ToList();

            // Default to the most recent snapshot month
            var latestSnapshot = await GetLatestSnapshotMonthAsync();
            var targetYear = latestSnapshot?.year ?? years[0];
            var targetMonth = latestSnapshot?.month ?? DateTime.Today.Month;

            // Set without triggering partial method reload — pickers aren't ready yet
            _selectedYear = targetYear;
            _selectedMonthName = AvailableMonths[targetMonth - 1];

            IsPickerReady = true;

            // Notify pickers so UI binds correctly
            OnPropertyChanged(nameof(SelectedYear));
            OnPropertyChanged(nameof(SelectedMonthName));

            await LoadSnapshotsAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Picker change handlers ────────────────────────────────────────────────

    partial void OnSelectedYearChanged(int value)
    {
        if (!IsPickerReady) return;
        _ = LoadSnapshotsAsync();
    }

    partial void OnSelectedMonthNameChanged(string value)
    {
        if (!IsPickerReady) return;
        _ = LoadSnapshotsAsync();
    }

    // ── Snapshot loader ───────────────────────────────────────────────────────

    private async Task LoadSnapshotsAsync()
    {
        if (SelectedYear == 0 || string.IsNullOrEmpty(SelectedMonthName)) return;

        IsLoading = true;
        try
        {
            var snapshots = await _snapshotRepo.GetByMonthAsync(SelectedYear, SelectedMonthIndex);

            if (snapshots.Count == 0)
            {
                HasData = false;
                OverallRow = null;
                CategoryRows = [];
                return;
            }

            HasData = true;

            // Overall row has null CategoryId
            var overallSnapshot = snapshots.FirstOrDefault(s => s.CategoryId is null);
            if (overallSnapshot is not null)
            {
                HasOverallLimit = overallSnapshot.Limit > 0;
                OverallRow = new BudgetHistoryRow
                {
                    CategoryName = "Overall",
                    Spent = overallSnapshot.Spent,
                    Limit = overallSnapshot.Limit,
                    WasExceeded = overallSnapshot.WasExceeded
                };
            }
            else
            {
                OverallRow = null;
                HasOverallLimit = false;
            }

            // Category rows — exclude the overall row, sort exceeded first then by name
            CategoryRows = snapshots
                .Where(s => s.CategoryId is not null)
                .OrderByDescending(s => s.WasExceeded)
                .ThenBy(s => s.CategoryName)
                .Select(s => new BudgetHistoryRow
                {
                    CategoryName = s.CategoryName ?? "Unknown",
                    Spent = s.Spent,
                    Limit = s.Limit,
                    WasExceeded = s.WasExceeded
                })
                .ToList();
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<(int year, int month)?> GetLatestSnapshotMonthAsync()
    {
        // Walk months 12->1 on the most recent year to find the latest
        // month that has an overall snapshot row
        if (AvailableYears.Count == 0) return null;

        var latestYear = AvailableYears[0]; // already sorted descending

        for (int m = 12; m >= 1; m--)
        {
            var found = await _snapshotRepo.GetAsync(latestYear, m, categoryId: null);
            if (found is not null) return (latestYear, m);
        }

        return null;
    }
}