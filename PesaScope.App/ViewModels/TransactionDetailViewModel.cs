using Android.Widget;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PesaScope.App.Data.Repositories.Interfaces;
using PesaScope.Core.Models;
using System.Collections.ObjectModel;

namespace PesaScope.App.ViewModels;

[QueryProperty(nameof(MpesaCode), "code")]
public partial class TransactionDetailViewModel : ObservableObject
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;

    // ── Query property ────────────────────────────────────────────────────────
    [ObservableProperty] private string _mpesaCode = string.Empty;

    // ── Data ──────────────────────────────────────────────────────────────────
    [ObservableProperty] private Transaction? _transaction;
    [ObservableProperty] private ObservableCollection<Category> _categories = [];
    [ObservableProperty] private Category? _selectedCategory;
    [ObservableProperty] private bool _isBusy;

    // ── Edit state ────────────────────────────────────────────────────────────
    [ObservableProperty] private bool _isNoteSheetOpen;
    [ObservableProperty] private bool _isCategorySheetOpen;
    [ObservableProperty] private string _editNote = string.Empty;

    // ── Derived display properties ────────────────────────────────────────────
    public string FormattedAmount => Transaction is null
        ? string.Empty
        : $"Ksh {Transaction.Amount:N0}";

    public string FormattedDate => Transaction?.TransactionDate.ToLocalTime()
        .ToString("ddd, d MMMM yyyy 'at' h:mm tt") ?? string.Empty;

    public string FormattedBalance => Transaction is null
        ? string.Empty
        : $"Ksh {Transaction.BalanceAfterTransaction:N0}";

    public bool IsCredit => Transaction?.Direction == TransactionDirection.Incoming;

    public Color AmountColor => IsCredit
        ? Color.FromArgb("#1A8C62")
        : Color.FromArgb("#C0392B");

    public string AmountPrefix => IsCredit ? "+" : "-";

    public TransactionDetailViewModel(
        ITransactionRepository transactionRepo,
        ICategoryRepository categoryRepo)
    {
        _transactionRepo = transactionRepo;
        _categoryRepo = categoryRepo;
    }

    // ── Load ──────────────────────────────────────────────────────────────────

    partial void OnMpesaCodeChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            _ = LoadAsync(value);
    }

    private async Task LoadAsync(string code)
    {
        IsBusy = true;
        try
        {
            var txTask = _transactionRepo.GetByMpesaCodeAsync(code);
            var catTask = _categoryRepo.GetAllActiveAsync();
            await Task.WhenAll(txTask, catTask);

            Transaction = txTask.Result;
            Categories = new ObservableCollection<Category>(catTask.Result);

            if (Transaction is not null)
            {
                SelectedCategory = Categories.FirstOrDefault(c => c.Id == Transaction.CategoryId);
                EditNote = Transaction.Note ?? string.Empty;
            }

            OnPropertyChanged(nameof(FormattedAmount));
            OnPropertyChanged(nameof(FormattedDate));
            OnPropertyChanged(nameof(FormattedBalance));
            OnPropertyChanged(nameof(AmountColor));
            OnPropertyChanged(nameof(AmountPrefix));
            OnPropertyChanged(nameof(IsCredit));
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── Category edit (3.8) ───────────────────────────────────────────────────

    [RelayCommand]
    public void OpenCategorySheet()
    {
        IsCategorySheetOpen = true;
    }

    [RelayCommand]
    public async Task SaveCategoryAsync()
    {
        if (Transaction is null || SelectedCategory is null) return;

        await _transactionRepo.UpdateCategoryAsync(Transaction.MpesaCode, SelectedCategory.Id);
        Transaction.CategoryId = SelectedCategory.Id;
        IsCategorySheetOpen = false;
    }

    // ── Note edit (3.9) ───────────────────────────────────────────────────────

    [RelayCommand]
    public void OpenNoteSheet()
    {
        EditNote = Transaction?.Note ?? string.Empty;
        IsNoteSheetOpen = true;
    }

    [RelayCommand]
    public async Task SaveNoteAsync()
    {
        if (Transaction is null) return;

        await _transactionRepo.UpdateNoteAsync(Transaction.MpesaCode, EditNote);
        Transaction.Note = EditNote;
        OnPropertyChanged(nameof(Transaction));
        IsNoteSheetOpen = false;
    }

    [RelayCommand]
    public void CloseSheet()
    {
        IsNoteSheetOpen = false;
        IsCategorySheetOpen = false;
    }

    [RelayCommand]
    public void SelectCategoryForEdit(Category category)
    {
        SelectedCategory = category;
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task GoBackAsync() => await Shell.Current.GoToAsync("..");

    [RelayCommand]
    public async Task CopySmsAsync()
    {
        if (Transaction is null || string.IsNullOrWhiteSpace(Transaction.OriginalSms))
            return;

        await Clipboard.Default.SetTextAsync(Transaction.OriginalSms);
    }

    [RelayCommand]
    public async Task CopyMpesaCodeAsync()
    {
        if (Transaction is null || string.IsNullOrWhiteSpace(Transaction.MpesaCode))
            return;

        await Clipboard.Default.SetTextAsync(Transaction.MpesaCode);
    }

    [RelayCommand]
    public async Task CopyCounterPartyAsync()
    {
        if (Transaction is null || string.IsNullOrWhiteSpace(Transaction.CounterpartyName))
            return;

        await Clipboard.Default.SetTextAsync(Transaction.CounterpartyName);
    }
}