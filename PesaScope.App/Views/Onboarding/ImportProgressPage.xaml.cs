using PesaScope.App.Data.Repositories.Interfaces;
using PesaScope.App.Services.Interfaces;
using PesaScope.Core.Models;
using PesaScope.Core.Services.Interfaces;

namespace PesaScope.App.Views.Onboarding;

public partial class ImportProgressPage : UraniumUI.Pages.UraniumContentPage
{
    private readonly IAppSettingsRepository _appSettingsRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ISyncMetadataRepository _syncMetadataRepo;
    private readonly ISmsReaderService _smsReader;
    private readonly IMpesaSmsParser _mpesaSmsParser;
    private readonly IAutoCategorizationService _autoCategorizationService;

    public ImportProgressPage(
        IAppSettingsRepository appSettingsRepo,
        ITransactionRepository transactionRepo,
        ISyncMetadataRepository syncMetadataRepo,
        ISmsReaderService smsReader,
        IMpesaSmsParser mpesaSmsParser,
        IAutoCategorizationService autoCategorizationService)
    {
        InitializeComponent();

        _appSettingsRepo = appSettingsRepo;
        _transactionRepo = transactionRepo;
        _syncMetadataRepo = syncMetadataRepo;
        _smsReader = smsReader;
        _mpesaSmsParser = mpesaSmsParser;
        _autoCategorizationService = autoCategorizationService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = RunImportAsync();
    }

    private async Task RunImportAsync()
    {
        try
        {
            var settings = await _appSettingsRepo.GetAsync();

            if (!settings.ImportComplete)
                await RunHistoricalImportAsync();
            else
                await FinishAsync(importedCount: 0, wasHistorical: false, noMessages: false);
        }
        catch (Exception ex)
        {
            await SetStatusAsync($"Something went wrong: {ex.Message}");
            ShowDoneButton("Continue Anyway");
        }
    }

    private async Task RunHistoricalImportAsync()
    {
        await SetStatusAsync("Reading messages from MPESA...");

        var messages = await _smsReader.GetAllMpesaMessagesAsync();

        if (messages is null || messages.Count == 0)
        {
            await FinishAsync(importedCount: 0, wasHistorical: true, noMessages: true);
            return;
        }

        await SetStatusAsync($"Found {messages.Count} M-Pesa messages. Parsing...");

        var (imported, duplicates) = await ParseAndImportAsync(messages);

        await FinishAsync(importedCount: imported, wasHistorical: true, duplicateCount: duplicates);
    }

    private async Task<(int Inserted, int Duplicates)> ParseAndImportAsync(
        List<PesaScope.App.Services.Interfaces.SmsMessage> messages)
    {
        var transactions = new List<Transaction>();
        int total = messages.Count;

        for (int i = 0; i < total; i++)
        {
            var msg = messages[i];
            var tx = _mpesaSmsParser.Parse(msg.Body, msg.SmsId, msg.Timestamp);

            if (tx is not null)
                transactions.Add(tx);

            if (i % 10 == 0 || i == total - 1)
            {
                double progress = (double)(i + 1) / total;
                int found = transactions.Count;

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ImportProgressBar.Progress = progress;
                    CountLabel.Text = $"{found} transaction{(found == 1 ? "" : "s")} found";
                    StatusLabel.Text = $"Parsing message {i + 1} of {total}...";
                });
            }
        }

        await SetStatusAsync("Saving to your device...");

        var (inserted, duplicates) = await _transactionRepo.InsertManyAsync(transactions);

        await SetStatusAsync("Categorizing transactions...");
        await _autoCategorizationService.CategorizeAsync(transactions);

        if (messages.Count > 0)
        {
            var last = messages[^1];
            await _syncMetadataRepo.UpdateAfterSyncAsync(last.SmsId, last.Timestamp, inserted);
        }

        return (inserted, duplicates);
    }

    private async Task FinishAsync(
        int importedCount,
        bool wasHistorical,
        bool noMessages = false,
        int duplicateCount = 0)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            ImportProgressBar.Progress = 1.0;

            if (noMessages)
            {
                StatusIcon.Text = "🤷";
                TitleLabel.Text = "No M-Pesa Messages Found";
                SubtitleLabel.Text =
                    "We couldn't find any MPESA messages in your inbox. " +
                    "Transactions will appear automatically after your next M-Pesa activity.";
                StatusLabel.Text = "You can also sync manually from Settings at any time.";
                CountLabel.IsVisible = false;
            }
            else if (!wasHistorical)
            {
                StatusIcon.Text = "⚡";
                TitleLabel.Text = "Ready to Go!";
                SubtitleLabel.Text =
                    "PesaScope will automatically capture new M-Pesa transactions as they arrive.";
                StatusLabel.Text = "Tip: you can re-sync from Settings at any time.";
                CountLabel.IsVisible = false;
            }
            else
            {
                StatusIcon.Text = "✅";
                TitleLabel.Text = "Import Complete!";
                SubtitleLabel.Text = "Your M-Pesa history is ready.";
                StatusLabel.Text = duplicateCount > 0
                    ? $"Successfully imported {importedCount} " +
                      $"transaction{(importedCount == 1 ? "" : "s")}. " +
                      $"Skipped {duplicateCount} duplicate{(duplicateCount == 1 ? "" : "s")}."
                    : $"Successfully imported {importedCount} " +
                      $"transaction{(importedCount == 1 ? "" : "s")}.";
                CountLabel.Text =
                    $"{importedCount} transaction{(importedCount == 1 ? "" : "s")} imported";
            }

            var settings = await _appSettingsRepo.GetAsync();
            settings.ImportComplete = true;
            await _appSettingsRepo.UpdateAsync(settings);

            ShowDoneButton("Go to Dashboard");
        });
    }

    private Task SetStatusAsync(string message) =>
        MainThread.InvokeOnMainThreadAsync(() => StatusLabel.Text = message);

    private void ShowDoneButton(string label)
    {
        DoneButton.Text = label;
        DoneButton.IsVisible = true;
    }

    private async void OnDoneClicked(object? sender, EventArgs e)
    {
        var settings = await _appSettingsRepo.GetAsync();
        settings.OnboardingComplete = true;
        settings.ImportComplete = true;
        await _appSettingsRepo.UpdateAsync(settings);

        if (Application.Current?.Windows.FirstOrDefault() is Window window)
            window.Page = new AppShell();
    }
}