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

    private bool _waitingForRestoreResult = false;

    /// <summary>
    /// Set by PermissionPage before navigating here.
    /// True  → user set PesaScope as default; we can bulk-import history.
    /// False → user skipped; we only capture future transactions.
    /// </summary>
    public bool HistoricalImportEnabled { get; set; }

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

        if (_waitingForRestoreResult)
        {
            // Returning from the system Default Apps settings screen
            _waitingForRestoreResult = false;

            if (!IsPesaLensStillDefault())
            {
                // User successfully switched back — hide the card
                RestoreDefaultCard.IsVisible = false;
                StatusLabel.Text = "✓ Default SMS app restored.";
            }
            else
            {
                // Still default — reset button so they can try again
                RestoreDefaultButton.IsEnabled = true;
                RestoreDefaultButton.Text = "Restore Default SMS App";
            }
        }
        else
        {
            // Normal first appearance — run the import
            _ = RunImportAsync();
        }
    }

    // ── Main import orchestration ─────────────────────────────────────────────

    private async Task RunImportAsync()
    {
        try
        {
            var settings = await _appSettingsRepo.GetAsync();

            if (HistoricalImportEnabled && !settings.ImportComplete)
                await RunHistoricalImportAsync();
            else
                await RunSkippedImportAsync();
        }
        catch (Exception ex)
        {
            await SetStatusAsync($"Something went wrong: {ex.Message}");
            ShowDoneButton("Continue Anyway");
        }
    }

    // ── Historical import (user set PesaScope as default) ─────────────────────

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

    // ── Skipped path (user chose not to set as default) ───────────────────────

    private async Task RunSkippedImportAsync()
    {
        await SetStatusAsync("Setting up PesaScope...");

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            ImportProgressBar.Progress = 1.0;
            CountLabel.IsVisible = false;
        });

        await FinishAsync(importedCount: 0, wasHistorical: false, false, IsPesaLensStillDefault());
    }

    // ── Parse + insert ────────────────────────────────────────────────────────
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

    // ── Completion ────────────────────────────────────────────────────────────
    private async Task FinishAsync(
        int importedCount,
        bool wasHistorical,
        bool noMessages = false,
        bool showRestoreCard = false,
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
                StatusLabel.Text =
                    "Tip: you can import your history later from Settings → Sync.";
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

            if (wasHistorical || showRestoreCard)
                RestoreDefaultCard.IsVisible = true;

            var settings = await _appSettingsRepo.GetAsync();
            settings.ImportComplete = true;
            await _appSettingsRepo.UpdateAsync(settings);

            ShowDoneButton("Go to Dashboard");
        });
    }

    // ── Restore default SMS app ───────────────────────────────────────────────

    private void OnRestoreDefaultClicked(object? sender, EventArgs e)
    {
        _waitingForRestoreResult = true;
        LaunchChangeDefaultDialog();
        RestoreDefaultButton.IsEnabled = false;
        RestoreDefaultButton.Text = "Opening system settings…";
    }

    private void OnRestoreLaterTapped(object? sender, EventArgs e)
    {
        RestoreDefaultCard.IsVisible = false;
    }

    private static void LaunchChangeDefaultDialog()
    {
        var activity = Platform.CurrentActivity;
        if (activity == null) return;

        // API 29+: open the Default Apps settings screen so the user
        // can pick their preferred SMS app. RoleManager has no API to
        // release a role — you can only direct the user to do it themselves.
        var intent = new Android.Content.Intent(
            Android.Provider.Settings.ActionManageDefaultAppsSettings);

        // Fallback for OEM ROMs that don't expose that screen
        if (activity.PackageManager?.ResolveActivity(intent, 0) == null)
        {
            intent = new Android.Content.Intent(
                Android.Provider.Settings.ActionApplicationDetailsSettings);
            intent.SetData(Android.Net.Uri.Parse(
                $"package:{Android.App.Application.Context.PackageName}"));
        }

        activity.StartActivity(intent);
    }

    // ── Done / navigate to dashboard ──────────────────────────────────────────

    private async void OnDoneClicked(object? sender, EventArgs e)
    {
        if (IsPesaLensStillDefault())
        {
            // Force them to restore before proceeding
            await DisplayAlertAsync(
                "One More Step",
                "Please switch back to your preferred SMS app before continuing. " +
                "Tap \"Restore Default SMS App\" above to do this.",
                "OK");
            return; // ← block navigation
        }

        await UpdateOnboardingCompleteState();

        if (Application.Current?.Windows.FirstOrDefault() is Window window)
            window.Page = new AppShell();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Task SetStatusAsync(string message) =>
        MainThread.InvokeOnMainThreadAsync(() => StatusLabel.Text = message);

    private void ShowDoneButton(string label)
    {
        DoneButton.Text = label;
        DoneButton.IsVisible = true;
    }

    private static bool IsPesaLensStillDefault()
    {
        var context = Android.App.Application.Context;

        // API 29+: use RoleManager — the same API used to request the role
        if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            var roleManager = context.GetSystemService(
                Android.Content.Context.RoleService) as Android.App.Roles.RoleManager;

            if (roleManager is not null)
                return roleManager.IsRoleHeld(Android.App.Roles.RoleManager.RoleSms);
        }

        // Fallback for older APIs
        var defaultPkg = Android.Provider.Telephony.Sms.GetDefaultSmsPackage(context);
        return defaultPkg == context.PackageName;
    }

    private async Task UpdateOnboardingCompleteState()
    {
        var settings = await _appSettingsRepo.GetAsync();
        settings.OnboardingComplete = true;
        settings.ImportComplete = true;
        await _appSettingsRepo.UpdateAsync(settings);
    }
}