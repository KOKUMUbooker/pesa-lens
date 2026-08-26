using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using PesaScope.App.Data.Repositories.Interfaces;
using PesaScope.App.Services.Interfaces;
using PesaScope.Core.Models;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;

namespace PesaScope.App.ViewModels;

public enum CategoryTypeFilter
{
    All = 0,
    SystemOnly = 1,
    UserDefinedOnly = 2
}

public enum CategorySortOption
{
    Alphabetical = 0,
    HighestAmount = 1,
    LowestAmount = 2
}

public class MonthOption
{
    public int Value { get; init; }
    public string Name { get; init; } = string.Empty;
    public override string ToString() => Name;
}

public partial class CategorySpendRow : ObservableObject
{
    public Category Category { get; init; } = null!;
    public decimal Amount { get; init; }
    public double Percentage { get; init; }
    public string FormattedAmount => $"Ksh {Amount:N0}";
    public string FormattedPercentage => $"{Percentage:0.#}%";
    public double ProgressValue => Percentage / 100.0;
    public Color ChartColor { get; init; } = Colors.Gray;
}

public partial class CategoriesViewModel : ObservableObject
{
    private readonly ICategoryRepository _categoryRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly IAutoCategorizationRuleRepository _rulesRepo;
    private readonly IAutoCategorizationService _autoCategorizationService;

    // Sentinel used to represent "no filter" in the rule category filter picker.
    private static readonly Category AllCategoriesOption = new()
    {
        Id = 0,
        Name = "All Categories"
    };

    // Unfiltered data as loaded from the repo. The [ObservableProperty] versions
    // (CategoryRows / Rules) hold the filtered/sorted view actually shown in the UI.
    private List<CategorySpendRow> _allCategoryRows = [];
    private List<AutoCategorizationRule> _allRules = [];

    [ObservableProperty] private ISeries[] _series = [];
    [ObservableProperty] private List<CategorySpendRow> _categoryRows = [];
    [ObservableProperty] private ObservableCollection<AutoCategorizationRule> _rules = [];
    [ObservableProperty] private bool _isBusy;

    // Individual loading states for each section
    [ObservableProperty] private bool _isChartLoading;
    [ObservableProperty] private bool _isCategoriesLoading;
    [ObservableProperty] private bool _isRulesLoading;
    [ObservableProperty] private bool _isChartEmpty;

    [ObservableProperty] private bool _isRefreshing;

    [ObservableProperty] private string _periodLabel = string.Empty;
    [ObservableProperty] private CategorySpendRow? _selectedCategory;
    [ObservableProperty] private CategorySpendRow? _incomeRow;

    // ── Accordion state for spending breakdown ───────────────────────────────
    [ObservableProperty] private bool _isChartExpanded = true;

    // ── Year / Month selectors ────────────────────────────────────────────────
    [ObservableProperty] private int _selectedYear;
    [ObservableProperty] private MonthOption _selectedMonth = null!;

    public List<int> AvailableYears { get; private set; } = [];
    public List<MonthOption> AvailableMonths { get; } =
        Enumerable.Range(1, 12)
            .Select(m => new MonthOption
            {
                Value = m,
                Name = new DateTime(2000, m, 1).ToString("MMMM")
            })
            .ToList();

    // ── Categories tab: search / filter / sort state ─────────────────────────
    [ObservableProperty] private string _categorySearchText = string.Empty;
    [ObservableProperty] private CategoryTypeFilter _categoryTypeFilter = CategoryTypeFilter.All;
    [ObservableProperty] private CategorySortOption _categorySortOption = CategorySortOption.HighestAmount;
    [ObservableProperty] private decimal? _minAmountFilter;
    [ObservableProperty] private decimal? _maxAmountFilter;
    [ObservableProperty] private bool _isCategoryFilterSheetOpen;
    [ObservableProperty] private bool _hasActiveCategoryFilters;
    [ObservableProperty] private string _categoriesEmptyMessage = "No categories to show.";

    // Draft copies edited inside the filter bottom sheet; only committed to the
    // properties above when the user taps "Apply", so amount thresholds don't
    // re-filter on every keystroke.
    [ObservableProperty] private string _draftMinAmountText = string.Empty;
    [ObservableProperty] private string _draftMaxAmountText = string.Empty;
    [ObservableProperty] private CategoryTypeFilter _draftCategoryTypeFilter = CategoryTypeFilter.All;
    [ObservableProperty] private CategorySortOption _draftCategorySortOption = CategorySortOption.HighestAmount;

    public List<CategoryTypeFilter> CategoryTypeFilterOptions { get; } = Enum.GetValues<CategoryTypeFilter>().ToList();
    public List<CategorySortOption> CategorySortOptions { get; } = Enum.GetValues<CategorySortOption>().ToList();

    // ── Rules tab: search / filter state ─────────────────────────────────────
    [ObservableProperty] private string _ruleSearchText = string.Empty;
    [ObservableProperty] private Category? _ruleFilterCategory;
    [ObservableProperty] private List<Category> _ruleFilterCategories = [AllCategoriesOption];
    [ObservableProperty] private string _rulesEmptyMessage = "No rules yet. Tap '+ Add rule' to create one.";

    // ── Add/Edit Category sheet state ────────────────────────────────────────
    [ObservableProperty] private bool _isSheetOpen;
    [ObservableProperty] private string _editName = string.Empty;
    [ObservableProperty] private string _editIcon = "label";
    [ObservableProperty] private string _editColor = "#1A8C62";
    [ObservableProperty] private Category? _editingCategory;

    // ── Add Rule sheet state ──────────────────────────────────────────────────
    [ObservableProperty] private bool _isRuleSheetOpen;
    [ObservableProperty] private string _ruleMatchValue = string.Empty;
    [ObservableProperty] private RuleType _ruleType = RuleType.ContainsText;
    [ObservableProperty] private Category? _ruleTargetCategory;
    [ObservableProperty] private AutoCategorizationRule? _editingRule;
    [ObservableProperty] private bool _isAutoCategorizing;

    // ── Resolved period (set by LoadChartAsync, read by the page for navigation) ──
    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd { get; private set; }

    public List<RuleType> RuleTypes { get; } = Enum.GetValues<RuleType>().ToList();

    public bool HasIncome => IncomeRow is not null;

    partial void OnIncomeRowChanged(CategorySpendRow? value) => OnPropertyChanged(nameof(HasIncome));

    public string AutoCategorizeButtonText => IsAutoCategorizing
        ? "Categorizing…"
        : "Auto-categorize transactions";

    partial void OnIsAutoCategorizingChanged(bool value) =>
        OnPropertyChanged(nameof(AutoCategorizeButtonText));

    public CategoriesViewModel(
        ICategoryRepository categoryRepo,
        ITransactionRepository transactionRepo,
        IAutoCategorizationRuleRepository rulesRepo,
        IAutoCategorizationService autoCategorizationService)
    {
        _categoryRepo = categoryRepo;
        _transactionRepo = transactionRepo;
        _rulesRepo = rulesRepo;
        _autoCategorizationService = autoCategorizationService;

        // Default the rule filter to "All Categories" so the picker has a sane initial selection.
        RuleFilterCategory = AllCategoriesOption;

        // Default to the current date; year range spans back 4 years for now,
        // which is generous for a personal-finance app with local SMS history.
        var today = DateTime.Today;
        AvailableYears = Enumerable.Range(today.Year - 4, 5).Reverse().ToList();
        _selectedYear = today.Year;
        _selectedMonth = AvailableMonths.First(m => m.Value == today.Month);
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var chartTask = LoadChartAsync();
            var categoriesTask = LoadCategoriesAsync();
            var rulesTask = LoadRulesAsync();

            await Task.WhenAll(chartTask, categoriesTask, rulesTask);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        IsRefreshing = true;
        try
        {
            await LoadAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    // ── Chart / category-row loading ──────────────────────────────────────────

    private async Task LoadChartAsync()
    {
        IsChartLoading = true;
        try
        {
            var from = new DateTime(SelectedYear, SelectedMonth.Value, 1);

            var isCurrentMonth = from.Year == DateTime.Today.Year && from.Month == DateTime.Today.Month;
            var to = isCurrentMonth
                ? DateTime.Today.AddDays(1).AddTicks(-1)
                : from.AddMonths(1).AddDays(-1).AddDays(1).AddTicks(-1);

            PeriodStart = from;
            PeriodEnd = to;
            PeriodLabel = from.ToString("MMMM yyyy");

            var categories = await _categoryRepo.GetAllActiveAsync();
            var spendMap = await _transactionRepo.GetSpendingByCategoryAsync(from, to); // outgoing-only now
            var incomeTotal = await _transactionRepo.GetTotalReceivedAsync(from, to);

            decimal total = spendMap.Values.Sum();

            var pieSeries = new List<ISeries>();
            var categoryRows = new List<CategorySpendRow>();
            CategorySpendRow? incomeRow = null;

            foreach (var cat in categories)
            {
                // Pull Income out into its own row instead of the spending list/chart.
                if (cat.Name == "Income")
                {
                    incomeRow = new CategorySpendRow
                    {
                        Category = cat,
                        Amount = incomeTotal,
                        Percentage = 0,
                        ChartColor = Color.FromArgb(cat.Color)
                    };
                    continue;
                }

                var amount = spendMap.GetValueOrDefault(cat.Id);
                var color = ParseColor(cat.Color);
                var pct = total > 0 ? (double)(amount / total) * 100 : 0;

                categoryRows.Add(new CategorySpendRow
                {
                    Category = cat,
                    Amount = amount,
                    Percentage = pct,
                    ChartColor = Color.FromArgb(cat.Color)
                });

                if (amount > 0)
                {
                    pieSeries.Add(new PieSeries<double>
                    {
                        Values = [(double)amount],
                        Name = cat.Name,
                        Fill = new SolidColorPaint(color),
                        Stroke = null,
                        OuterRadiusOffset = 0,
                        MaxRadialColumnWidth = 28,
                        ToolTipLabelFormatter = p => $"{cat.Name}: Ksh {amount:N0}"
                    });
                }
            }

            Series = [.. pieSeries];
            _allCategoryRows = categoryRows;
            IsChartEmpty = total <= 0;
            IncomeRow = incomeRow;

            ApplyCategoryDisplayFilters();
        }
        finally
        {
            IsChartLoading = false;
        }
    }

    private async Task LoadCategoriesAsync()
    {
        IsCategoriesLoading = true;
        try
        {
            await Task.CompletedTask; // Placeholder for future category-specific loading
        }
        finally
        {
            IsCategoriesLoading = false;
        }
    }

    // ── Rules loading ─────────────────────────────────────────────────────────
    private async Task LoadRulesAsync()
    {
        IsRulesLoading = true;
        try
        {
            var result = await _rulesRepo.GetEnabledOrderedByPriorityAsync();
            var categories = await _categoryRepo.GetAllAsync();
            var catMap = categories.ToDictionary(c => c.Id);

            foreach (var rule in result)
                rule.Category = catMap.GetValueOrDefault(rule.CategoryId);

            _allRules = result;

            RuleFilterCategories =
            [
                AllCategoriesOption,
                .. result
                    .Where(r => r.Category is not null)
                    .Select(r => r.Category!)
                    .DistinctBy(c => c.Id)
                    .OrderBy(c => c.Name)
            ];

            RuleFilterCategory = RuleFilterCategories
                .FirstOrDefault(c => c.Id == RuleFilterCategory?.Id) ?? AllCategoriesOption;

            ApplyRuleFilters();
        }
        finally
        {
            IsRulesLoading = false;
        }
    }

    partial void OnRuleSearchTextChanged(string value) => ApplyRuleFilters();

    partial void OnRuleFilterCategoryChanged(Category? value) => ApplyRuleFilters();

    private void ApplyRuleFilters()
    {
        IEnumerable<AutoCategorizationRule> filtered = _allRules;

        if (RuleFilterCategory is not null && RuleFilterCategory.Id != 0)
            filtered = filtered.Where(r => r.CategoryId == RuleFilterCategory.Id);

        if (!string.IsNullOrWhiteSpace(RuleSearchText))
        {
            var search = RuleSearchText.Trim();
            filtered = filtered.Where(r =>
                r.MatchValue.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                r.RuleType.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (r.Category?.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        Rules = new ObservableCollection<AutoCategorizationRule>(filtered);

        RulesEmptyMessage = _allRules.Count == 0
            ? "No rules yet. Tap '+ Add rule' to create one."
            : "No rules match your search or filter.";
    }

    [RelayCommand]
    public void ClearRuleFilters()
    {
        RuleSearchText = string.Empty;
        RuleFilterCategory = AllCategoriesOption;
    }

    [RelayCommand]
    public void ToggleChartExpanded() => IsChartExpanded = !IsChartExpanded;

    // ── Categories tab: search / filter / sort ────────────────────────────────

    partial void OnCategorySearchTextChanged(string value) => ApplyCategoryDisplayFilters();

    [RelayCommand]
    public void OpenCategoryFilterSheet()
    {
        // Seed the draft from the currently-applied filters so the sheet
        // reopens showing what's actually in effect.
        DraftMinAmountText = MinAmountFilter?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        DraftMaxAmountText = MaxAmountFilter?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        DraftCategoryTypeFilter = CategoryTypeFilter;
        DraftCategorySortOption = CategorySortOption;
        IsCategoryFilterSheetOpen = true;
    }

    [RelayCommand]
    public void CloseCategoryFilterSheet()
    {
        // Discard any unapplied draft edits.
        IsCategoryFilterSheetOpen = false;
    }

    [RelayCommand]
    public void ApplyCategoryFilters()
    {
        decimal? min = decimal.TryParse(DraftMinAmountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var minVal)
            ? minVal
            : null;
        decimal? max = decimal.TryParse(DraftMaxAmountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var maxVal)
            ? maxVal
            : null;

        MinAmountFilter = min;
        MaxAmountFilter = max;
        CategoryTypeFilter = DraftCategoryTypeFilter;
        CategorySortOption = DraftCategorySortOption;

        IsCategoryFilterSheetOpen = false;
        ApplyCategoryDisplayFilters();
    }

    [RelayCommand]
    public void ResetCategoryFilters()
    {
        DraftMinAmountText = string.Empty;
        DraftMaxAmountText = string.Empty;
        DraftCategoryTypeFilter = CategoryTypeFilter.All;
        DraftCategorySortOption = CategorySortOption.HighestAmount;

        MinAmountFilter = null;
        MaxAmountFilter = null;
        CategoryTypeFilter = CategoryTypeFilter.All;
        CategorySortOption = CategorySortOption.HighestAmount;

        IsCategoryFilterSheetOpen = false;
        ApplyCategoryDisplayFilters();
    }

    private void ApplyCategoryDisplayFilters()
    {
        IEnumerable<CategorySpendRow> filtered = _allCategoryRows;

        if (!string.IsNullOrWhiteSpace(CategorySearchText))
        {
            var search = CategorySearchText.Trim();
            filtered = filtered.Where(r =>
                r.Category.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (MinAmountFilter is decimal min)
            filtered = filtered.Where(r => r.Amount >= min);

        if (MaxAmountFilter is decimal max)
            filtered = filtered.Where(r => r.Amount <= max);

        filtered = CategoryTypeFilter switch
        {
            CategoryTypeFilter.SystemOnly => filtered.Where(r => r.Category.IsSystemCategory),
            CategoryTypeFilter.UserDefinedOnly => filtered.Where(r => !r.Category.IsSystemCategory),
            _ => filtered
        };

        filtered = CategorySortOption switch
        {
            CategorySortOption.HighestAmount => filtered.OrderByDescending(r => r.Amount).ThenBy(r => r.Category.Name),
            CategorySortOption.LowestAmount => filtered.OrderBy(r => r.Amount).ThenBy(r => r.Category.Name),
            _ => filtered.OrderBy(r => r.Category.Name)
        };

        CategoryRows = filtered.ToList();

        HasActiveCategoryFilters =
            !string.IsNullOrWhiteSpace(CategorySearchText) ||
            MinAmountFilter is not null ||
            MaxAmountFilter is not null ||
            CategoryTypeFilter != CategoryTypeFilter.All ||
            CategorySortOption != CategorySortOption.HighestAmount;

        CategoriesEmptyMessage = _allCategoryRows.Count == 0
            ? "No categories to show."
            : "No categories match your search or filters.";
    }

    [RelayCommand]
    public async Task AutoCategorizeAllAsync()
    {
        if (IsAutoCategorizing) return;

        bool confirmed = await Shell.Current.DisplayAlertAsync(
            "Auto-Categorize Transactions",
            "This applies your current rules to every transaction, overwriting the category on any transaction that matches a rule. This can't be undone.",
            "Continue",
            "Cancel");

        if (!confirmed) return;

        IsAutoCategorizing = true;
        try
        {
            int updated = await _autoCategorizationService.RecategorizeAllAsync();

            await Shell.Current.DisplayAlertAsync(
                "Done",
                updated == 0
                    ? "No transactions matched your rules."
                    : $"{updated} transaction(s) were categorized.",
                "OK");

            await LoadAsync();
        }
        finally
        {
            IsAutoCategorizing = false;
        }
    }

    // ── Add / Edit category (4.4, 4.5) ───────────────────────────────────────

    [RelayCommand]
    public void OpenAddCategory()
    {
        EditingCategory = null;
        EditName = string.Empty;
        EditIcon = "label";
        EditColor = "#1A8C62";
        IsSheetOpen = true;
    }

    [RelayCommand]
    public void OpenEditCategory(CategorySpendRow row)
    {
        EditingCategory = row.Category;
        EditName = row.Category.Name;
        EditIcon = row.Category.Icon;
        EditColor = row.Category.Color;
        IsSheetOpen = true;
    }

    [RelayCommand]
    public async Task OpenIncomeAsync()
    {
        if (IncomeRow is null) return;

        await Shell.Current.GoToAsync(
            $"//Transactions/TransactionsPage?categoryId={IncomeRow.Category.Id}" +
            $"&fromDate={PeriodStart:yyyy-MM-dd}&toDate={PeriodEnd:yyyy-MM-dd}");
    }

    [RelayCommand]
    public async Task SaveCategoryAsync()
    {
        if (string.IsNullOrWhiteSpace(EditName)) return;

        var trimmedName = EditName.Trim();

        bool isDuplicateCheckNeeded = EditingCategory is null
            || !EditingCategory.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase);

        if (isDuplicateCheckNeeded && await _categoryRepo.GetByNameAsync(trimmedName) is not null)
        {
            await Shell.Current.DisplayAlertAsync(
                "Category Already Exists",
                $"A category named '{trimmedName}' already exists.",
                "OK");
            return;
        }

        bool success;

        if (EditingCategory is null)
        {
            success = await _categoryRepo.TryInsertAsync(new Category
            {
                Name = trimmedName,
                Icon = EditIcon,
                Color = EditColor,
                IsSystemCategory = false,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            EditingCategory.Name = trimmedName;
            EditingCategory.Icon = EditIcon;
            EditingCategory.Color = EditColor;
            success = await _categoryRepo.TryUpdateAsync(EditingCategory);
        }

        if (!success)
        {
            await Shell.Current.DisplayAlertAsync(
                "Category Already Exists",
                $"A category named '{trimmedName}' already exists.",
                "OK");
            return;
        }

        IsSheetOpen = false;
        await LoadAsync();
    }

    [RelayCommand]
    public async Task DeleteCategoryAsync(CategorySpendRow row)
    {
        if (row.Category.IsSystemCategory) return;

        bool confirmed = await Shell.Current.DisplayAlertAsync(
            "Delete Category",
            $"Transactions in '{row.Category.Name}' will be moved to Uncategorized, and any linked rules or budgets will be removed. This can't be undone.",
            "Delete",
            "Cancel");

        if (!confirmed) return;

        await _categoryRepo.DeleteAndReassignAsync(row.Category.Id);
        await LoadAsync();
    }

    // ── Add / Edit rule (4.6) ─────────────────────────────────────────────────

    [RelayCommand]
    public void OpenAddRule()
    {
        EditingRule = null;
        RuleMatchValue = string.Empty;
        RuleType = RuleType.ContainsText;
        RuleTargetCategory = null;
        IsRuleSheetOpen = true;
    }

    [RelayCommand]
    public void OpenEditRule(AutoCategorizationRule rule)
    {
        EditingRule = rule;
        RuleMatchValue = rule.MatchValue;
        RuleType = rule.RuleType;
        IsRuleSheetOpen = true;
    }

    [RelayCommand]
    public async Task SaveRuleAsync()
    {
        if (string.IsNullOrWhiteSpace(RuleMatchValue) || RuleTargetCategory is null) return;

        var trimmedValue = RuleMatchValue.Trim();

        bool isDuplicateCheckNeeded = EditingRule is null
            || EditingRule.RuleType != RuleType
            || !EditingRule.MatchValue.Equals(trimmedValue, StringComparison.OrdinalIgnoreCase);

        if (isDuplicateCheckNeeded && await _rulesRepo.ExistsAsync(RuleType, trimmedValue))
        {
            await Shell.Current.DisplayAlertAsync(
                "Rule Already Exists",
                $"A rule already exists for '{trimmedValue}' with this rule type.",
                "OK");
            return;
        }

        bool success;

        if (EditingRule is null)
        {
            success = await _rulesRepo.TryInsertAsync(new AutoCategorizationRule
            {
                RuleType = RuleType,
                MatchValue = trimmedValue,
                CategoryId = RuleTargetCategory.Id,
                Priority = 5,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            EditingRule.RuleType = RuleType;
            EditingRule.MatchValue = trimmedValue;
            EditingRule.CategoryId = RuleTargetCategory!.Id;
            success = await _rulesRepo.TryUpdateAsync(EditingRule);
        }

        if (!success)
        {
            // Defense-in-depth: the checks above should already have caught this,
            // but the unique index is the real source of truth.
            await Shell.Current.DisplayAlertAsync(
                "Rule Already Exists",
                $"A rule already exists for '{trimmedValue}' with this rule type.",
                "OK");
            return;
        }

        IsRuleSheetOpen = false;
        await LoadRulesAsync();
    }

    [RelayCommand]
    public async Task DeleteRuleAsync(AutoCategorizationRule rule)
    {
        bool confirmed = await Shell.Current.DisplayAlertAsync(
            "Delete Rule",
            $"Are you sure you want to delete this rule as this can't be undone ?",
            "Delete",
            "Cancel");

        if (!confirmed) return;

        await _rulesRepo.DeleteAsync(rule);
        await LoadRulesAsync();
    }

    [RelayCommand]
    public void CloseSheet()
    {
        IsSheetOpen = false;
        IsRuleSheetOpen = false;
    }

    partial void OnSelectedYearChanged(int value) => _ = LoadChartAsync();

    partial void OnSelectedMonthChanged(MonthOption value) => _ = LoadChartAsync();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SKColor ParseColor(string hex)
    {
        try { return SKColor.Parse(hex); }
        catch { return SKColor.Parse("#90A4AE"); }
    }
}