using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PesaScope.App.Data.Repositories.Interfaces;
using PesaScope.Core.Models;

namespace PesaScope.App.ViewModels.Categories;

/// <summary>
/// Drives both "Add category" (categoryId == null) and
/// "Edit category" (categoryId provided) from the same page.
/// </summary>
public partial class EditCategoryViewModel : ObservableObject, IQueryAttributable
{
    private readonly ICategoryRepository _categories;

    // ── Mode ─────────────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageTitle))]
    [NotifyPropertyChangedFor(nameof(CanDelete))]
    private bool _isEditMode = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDelete))]
    private bool _isSystemCategory = false;

    public string PageTitle => IsEditMode ? "Edit category" : "New category";
    public bool CanDelete => IsEditMode && !IsSystemCategory;

    // ── Form fields ───────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private string _selectedIcon = "🏷️";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private string _selectedColor = "#1A8C62";

    // Validation
    [ObservableProperty] private string _nameError = string.Empty;
    public bool CanSave => Name.Trim().Length > 0 && !string.IsNullOrEmpty(SelectedIcon);

    // ── Picker state ──────────────────────────────────────────────────────────
    [ObservableProperty] private bool _isIconPickerVisible = false;
    [ObservableProperty] private bool _isColorPickerVisible = false;

    // ── Picker data ───────────────────────────────────────────────────────────

    /// <summary>All icons the user can choose from, grouped by label.</summary>
    public List<IconGroup> IconGroups { get; } =
    [
        new("Finance",   ["💳", "💵", "💰", "🏦", "💸", "🪙", "📈", "📉", "🏧", "💹"]),
        new("Food",      ["🍔", "🍕", "🛒", "🍜", "🥘", "🍱", "☕", "🍺", "🥗", "🍎"]),
        new("Transport", ["🚗", "🚌", "✈️", "⛽", "🛵", "🚂", "🚕", "🚲", "🛺", "⚓"]),
        new("Home",      ["🏠", "🛋️", "💡", "🔧", "🪣", "🛁", "🔑", "🏗️", "🪟", "🧹"]),
        new("Health",    ["❤️", "💊", "🏥", "🧘", "🏋️", "🩺", "🦷", "🩹", "🧬", "🚑"]),
        new("Education", ["📚", "🎓", "✏️", "🖥️", "🔬", "🎨", "🎭", "🎵", "📐", "🗺️"]),
        new("Shopping",  ["👗", "👟", "💄", "📦", "🛍️", "🎁", "⌚", "👜", "💻", "📱"]),
        new("Misc",      ["🌟", "🎮", "⚡", "🔔", "📌", "🧩", "🪴", "🐾", "🌍", "🎉"]),
    ];

    /// <summary>Flat palette of hex colours for the colour picker grid.</summary>
    public List<string> ColorPalette { get; } =
    [
        // Greens (brand)
        "#1A8C62", "#2ECC71", "#27AE60", "#0D8F5F", "#00695C", "#4CAF50",
        // Blues
        "#2196F3", "#1565C0", "#42A5F5", "#0288D1", "#5C6BC0", "#3F51B5",
        // Reds / Oranges
        "#D4522A", "#E53935", "#F44336", "#FF7043", "#F57C00", "#FF8F00",
        // Yellows / Ambers
        "#C98A00", "#FFC107", "#FFB300", "#FF8F00", "#F9A825", "#F57F17",
        // Purples / Pinks
        "#AB47BC", "#7B1FA2", "#9C27B0", "#EC407A", "#AD1457", "#E91E63",
        // Teals / Cyans
        "#26A69A", "#00897B", "#009688", "#00BCD4", "#0097A7", "#006064",
        // Browns / Greys
        "#8D6E63", "#795548", "#6D4C41", "#90A4AE", "#607D8B", "#546E7A",
    ];

    // ── Internals ─────────────────────────────────────────────────────────────
    private int _categoryId;

    [ObservableProperty] private bool _isBusy = false;
    [ObservableProperty] private string _error = string.Empty;

    // ── Constructor ───────────────────────────────────────────────────────────
    public EditCategoryViewModel(ICategoryRepository categories)
    {
        _categories = categories;
    }

    // ── IQueryAttributable ────────────────────────────────────────────────────
    // Navigation: GoToAsync("EditCategoryPage?categoryId=5")  -> edit mode
    //             GoToAsync("EditCategoryPage")               -> add mode
    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("categoryId", out var raw) && int.TryParse(raw?.ToString(), out var id))
        {
            _categoryId = id;
            await LoadCategoryAsync(id);
        }
    }

    private async Task LoadCategoryAsync(int id)
    {
        IsBusy = true;
        try
        {
            var cat = await _categories.GetByIdAsync(id);
            if (cat is null) return;

            IsEditMode = true;
            IsSystemCategory = cat.IsSystemCategory;
            Name = cat.Name;
            SelectedIcon = cat.Icon;
            SelectedColor = cat.Color;
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── Icon picker ───────────────────────────────────────────────────────────
    [RelayCommand]
    private void ToggleIconPicker()
    {
        IsIconPickerVisible = !IsIconPickerVisible;
        IsColorPickerVisible = false;           // close the other picker
    }

    [RelayCommand]
    private void SelectIcon(string icon)
    {
        SelectedIcon = icon;
        IsIconPickerVisible = false;
    }

    // ── Color picker ──────────────────────────────────────────────────────────
    [RelayCommand]
    private void ToggleColorPicker()
    {
        IsColorPickerVisible = !IsColorPickerVisible;
        IsIconPickerVisible = false;           // close the other picker
    }

    [RelayCommand]
    private void SelectColor(string hex)
    {
        SelectedColor = hex;
        IsColorPickerVisible = false;
    }

    // ── Save ──────────────────────────────────────────────────────────────────
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        NameError = string.Empty;

        var trimmed = Name.Trim();
        if (trimmed.Length == 0)
        {
            NameError = "Category name is required.";
            return;
        }

        // Duplicate name check (skip own name in edit mode)
        var existing = await _categories.GetByNameAsync(trimmed);
        if (existing is not null && existing.Id != _categoryId)
        {
            NameError = "A category with this name already exists.";
            return;
        }

        IsBusy = true;
        try
        {
            if (IsEditMode)
            {
                var cat = await _categories.GetByIdAsync(_categoryId);
                if (cat is null) return;

                cat.Name = trimmed;
                cat.Icon = SelectedIcon;
                cat.Color = SelectedColor;
                await _categories.UpdateAsync(cat);
            }
            else
            {
                var cat = new Category
                {
                    Name = trimmed,
                    Icon = SelectedIcon,
                    Color = SelectedColor,
                    IsSystemCategory = false,
                    CreatedAt = DateTime.UtcNow,
                };
                await _categories.InsertAsync(cat);
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            Error = $"Could not save category: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── Delete ────────────────────────────────────────────────────────────────
    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync()
    {
        bool confirmed = await Shell.Current.DisplayAlertAsync(
            "Delete category",
            $"Delete \"{Name}\"? All transactions in this category will be moved to Uncategorized.",
            "Delete",
            "Cancel");

        if (!confirmed) return;

        IsBusy = true;
        try
        {
            await _categories.DeleteAndReassignAsync(_categoryId);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            Error = $"Could not delete category: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── Cancel ────────────────────────────────────────────────────────────────
    [RelayCommand]
    private static async Task CancelAsync() =>
        await Shell.Current.GoToAsync("..");
}

// ── Supporting types ──────────────────────────────────────────────────────────

/// <summary>A labelled group of emoji icons shown as a section in the picker.</summary>
public class IconGroup(string label, IEnumerable<string> icons)
{
    public string Label { get; } = label;
    public List<string> Icons { get; } = icons.ToList();
}