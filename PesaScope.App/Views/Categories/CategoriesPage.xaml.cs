using PesaScope.App.ViewModels;
using PesaScope.Core.Models;

namespace PesaScope.App.Views.Categories;

public partial class CategoriesPage : UraniumUI.Pages.UraniumContentPage
{
    private readonly CategoriesViewModel _vm;
    private bool _loaded;

    private static readonly string[] ColorOptions =
    [
        "#1A8C62", "#C98A00", "#D4522A", "#5C6BC0",
        "#AB47BC", "#EF5350", "#42A5F5", "#EC407A",
        "#8D6E63", "#26A69A", "#90A4AE", "#F06292"
    ];

    public CategoriesPage(CategoriesViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
        BuildColorPicker();

        // Repopulate rule picker whenever IsRefreshing goes false (refresh completed)
        _vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(CategoriesViewModel.IsRefreshing) && !_vm.IsRefreshing)
                PopulateRuleCategoryPicker();
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded) return;
        _loaded = true;

        try
        {
            await _vm.LoadAsync();
        }
        finally
        {
            // Populate picker regardless, even if load partially failed
            PopulateRuleCategoryPicker();
        }
    }

    private async void OnHelpTapped(object sender, EventArgs e)
    {
        await Shell.Current.Navigation.PushModalAsync(new CategoriesHelpPage());
    }

    // ── Color picker ──────────────────────────────────────────────────────────

    private void BuildColorPicker()
    {
        foreach (var hex in ColorOptions)
        {
            var swatch = new Border
            {
                WidthRequest = 32,
                HeightRequest = 32,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
                StrokeThickness = 0,
                BackgroundColor = Color.FromArgb(hex),
                Margin = new Thickness(0, 0, 4, 0)
            };

            var captured = hex;
            swatch.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() =>
                {
                    _vm.EditColor = captured;
                    RefreshColorPickerSelection();
                })
            });

            ColorPickerLayout.Children.Add(swatch);
        }
    }

    private void RefreshColorPickerSelection()
    {
        foreach (Border swatch in ColorPickerLayout.Children.OfType<Border>())
        {
            var hex = swatch.BackgroundColor?.ToHex();
            swatch.Stroke = hex != null &&
                hex.Equals(_vm.EditColor.TrimStart('#'), StringComparison.OrdinalIgnoreCase)
                ? new SolidColorBrush(Color.FromArgb(_vm.EditColor))
                : null;
            swatch.StrokeThickness = swatch.Stroke is not null ? 2.5 : 0;
        }
    }

    private async void OnCategoryItemTapped(object sender, EventArgs e)
    {
        if (sender is ImageButton btn && btn.BindingContext is CategorySpendRow row)
        {
            await Shell.Current.GoToAsync(
                $"//Transactions/TransactionsPage?categoryId={row.Category.Id}" +
                $"&fromDate={_vm.PeriodStart:yyyy-MM-dd}&toDate={_vm.PeriodEnd:yyyy-MM-dd}");
        }
    }

    // ── Rule category picker ──────────────────────────────────────────────────

    private void PopulateRuleCategoryPicker()
    {
        RuleCategoryPicker.ItemsSource = _vm.CategoryRows
            .Select(r => r.Category)
            .ToList();
    }

    private void OnRuleCategorySelected(object? sender, EventArgs e)
    {
        if (RuleCategoryPicker.SelectedItem is Category cat)
            _vm.RuleTargetCategory = cat;
    }
}