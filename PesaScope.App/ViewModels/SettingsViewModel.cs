using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PesaScope.App.Data;
using PesaScope.App.Data.Repositories.Interfaces;
using PesaScope.App.Services.Interfaces;
using PesaScope.App.Views.Onboarding;
using PesaScope.App.Views.Settings;
using PesaScope.Core.Models;
using PesaScope.Core.Services.Interfaces;
using AppTheme = PesaScope.Core.Models.AppTheme;

namespace PesaScope.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IAppSettingsRepository _appSettingsRepo;
    private readonly ISyncMetadataRepository _syncMetadataRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ISmsReaderService _smsReader;
    private readonly IMpesaSmsParser _mpesaSmsParser;
    private readonly IAutoCategorizationService _autoCategorizationService;
    private readonly DatabaseSeeder _seeder;
    private readonly DatabaseService _databaseService;
    private readonly IBiometricAuthService _biometricAuthService;
    private readonly IServiceProvider _services;

    private AppSettings _appSettings = new();

    // ── General ───────────────────────────────────────────────────────────────

    [ObservableProperty] private AppTheme _currentTheme;
    [ObservableProperty] private bool _budgetNotificationsEnabled;
    [ObservableProperty] private string _currencyDisplay = "Ksh";
    [ObservableProperty] private string _lastSyncedText = "Never";
    [ObservableProperty] private bool _isSyncing;
    [ObservableProperty] private bool _transactionNotificationsEnabled;

    // ── Security ──────────────────────────────────────────────────────────────

    [ObservableProperty] private bool _appLockEnabled;

    // ── About ─────────────────────────────────────────────────────────────────
    public string AppVersion =>
        AppInfo.VersionString is { } v ? $"v{v} ({AppInfo.BuildString})" : "v1.0.2";

    // ── Developer info ────────────────────────────────────────────────────────

    public string DeveloperName => "Booker Okumu";
    private const string PortfolioUrl = "https://bkokumu.com";
    private const string GitHubUrl = "https://github.com/KOKUMUbooker";
    private const string AppGitHubUrl = "https://github.com/KOKUMUbooker/pesa-scope";
    private const string AppWebsiteUrl = "https://pesascope.bkokumu.com";

    public SettingsViewModel(
        IAppSettingsRepository appSettingsRepo,
        ISyncMetadataRepository syncMetadataRepo,
        ITransactionRepository transactionRepo,
        ISmsReaderService smsReader,
        IMpesaSmsParser mpesaSmsParser,
        IAutoCategorizationService autoCategorizationService,
        DatabaseSeeder seeder,
        IBiometricAuthService biometricAuthService,
        IServiceProvider services,
        DatabaseService databaseService)
    {
        _appSettingsRepo = appSettingsRepo;
        _syncMetadataRepo = syncMetadataRepo;
        _transactionRepo = transactionRepo;
        _smsReader = smsReader;
        _mpesaSmsParser = mpesaSmsParser;
        _autoCategorizationService = autoCategorizationService;
        _seeder = seeder;
        _biometricAuthService = biometricAuthService;
        _databaseService = databaseService;
        _services = services;
    }

    // ── Load ──────────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadAsync()
    {
        _appSettings = await _appSettingsRepo.GetAsync();
        var syncMeta = await _syncMetadataRepo.GetAsync();

        // Populate observable properties without triggering saves
        CurrentTheme = _appSettings.Theme;
        BudgetNotificationsEnabled = _appSettings.BudgetNotificationsEnabled;
        AppLockEnabled = _appSettings.AppLockEnabled;
        TransactionNotificationsEnabled = _appSettings.TransactionNotificationsEnabled;

        CurrencyDisplay = _appSettings.CurrencyDisplay == PesaScope.Core.Models.CurrencyDisplay.Ksh ? "Ksh" : "KES";

        LastSyncedText = syncMeta.LastSyncTime == DateTime.MinValue
            ? "Never"
            : FormatSyncTime(syncMeta.LastSyncTime);
    }

    // ── Toggle handlers ───────────────────────────────────────────────────────

    [RelayCommand]
    public async Task SetThemeAsync(AppTheme theme)
    {
        CurrentTheme = theme;
        _appSettings.Theme = theme;

        Application.Current!.UserAppTheme = theme switch
        {
            AppTheme.Light => Microsoft.Maui.ApplicationModel.AppTheme.Light,
            AppTheme.Dark => Microsoft.Maui.ApplicationModel.AppTheme.Dark,
            _ => Microsoft.Maui.ApplicationModel.AppTheme.Unspecified
        };

        await _appSettingsRepo.UpdateAsync(_appSettings);
    }

    [RelayCommand]
    public async Task ToggleBudgetNotificationsAsync(bool value)
    {
        if (value)
        {
            // Request permission when user tries to enable notifications
            var status = await Permissions.RequestAsync<Permissions.PostNotifications>();

            if (status != PermissionStatus.Granted)
            {
                // Revert the toggle — permission was denied
                BudgetNotificationsEnabled = false;
                await Shell.Current.DisplayAlertAsync(
                    "Permission Required",
                    "Notification permission was denied. Enable it in your device settings to receive budget alerts.",
                    "OK");
                return;
            }
        }

        BudgetNotificationsEnabled = value;
        _appSettings.BudgetNotificationsEnabled = value;
        await _appSettingsRepo.UpdateAsync(_appSettings);
    }

    [RelayCommand]
    public async Task ToggleTransactionNotificationsAsync(bool value)
    {
        if (value)
        {
            var status = await Permissions.RequestAsync<Permissions.PostNotifications>();

            if (status != PermissionStatus.Granted)
            {
                TransactionNotificationsEnabled = false;
                await Shell.Current.DisplayAlertAsync(
                    "Permission Required",
                    "Notification permission was denied. Enable it in your device settings to receive transaction alerts.",
                    "OK");
                return;
            }
        }

        TransactionNotificationsEnabled = value;
        _appSettings.TransactionNotificationsEnabled = value;
        await _appSettingsRepo.UpdateAsync(_appSettings);
    }

    [RelayCommand]
    public async Task SetCurrencyAsync(string value)
    {
        CurrencyDisplay = value;
        _appSettings.CurrencyDisplay = value == "Ksh"
            ? PesaScope.Core.Models.CurrencyDisplay.Ksh
            : PesaScope.Core.Models.CurrencyDisplay.KES;
        await _appSettingsRepo.UpdateAsync(_appSettings);
    }

    [RelayCommand]
    public async Task ToggleAppLockAsync(bool value)
    {
        if (value)
        {
            var available = await _biometricAuthService.IsAvailableAsync();
            if (!available)
            {
                AppLockEnabled = false; // revert
                await Shell.Current.DisplayAlertAsync(
                    "Unavailable",
                    "Set up a screen lock (PIN, pattern, fingerprint, or face) on your device first.",
                    "OK");
                return;
            }
        }

        AppLockEnabled = value;
        _appSettings.AppLockEnabled = value;
        await _appSettingsRepo.UpdateAsync(_appSettings);
    }


    // ── Data management ───────────────────────────────────────────────────────

    [RelayCommand]
    public async Task SyncNowAsync()
    {
        if (IsSyncing) return;
        IsSyncing = true;

        try
        {
            var hasPermission = await _smsReader.HasPermissionAsync();
            if (!hasPermission)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Permission Needed",
                    "PesaScope needs SMS read permission to sync your M-Pesa messages. " +
                    "Please grant it in your device's app settings.",
                    "OK");
                return;
            }

            var syncMeta = await _syncMetadataRepo.GetAsync();
            var newMessages = await _smsReader.GetNewMpesaMessagesAsync(syncMeta.LastSmsId);

            if (newMessages is null || newMessages.Count == 0)
            {
                LastSyncedText = FormatSyncTime(DateTime.UtcNow);
                await Shell.Current.DisplayAlertAsync(
                    "Sync Complete",
                    "You're all caught up — no new transactions found.",
                    "OK");
                return;
            }

            var transactions = new List<Transaction>();
            foreach (var msg in newMessages)
            {
                var tx = _mpesaSmsParser.Parse(msg.Body, msg.SmsId, msg.Timestamp);
                if (tx is not null)
                    transactions.Add(tx);
            }

            int inserted = await _transactionRepo.InsertManyAsync(transactions);
            await _autoCategorizationService.CategorizeAsync(transactions);

            var last = newMessages[^1];
            await _syncMetadataRepo.UpdateAfterSyncAsync(last.SmsId, last.Timestamp, inserted);

            LastSyncedText = FormatSyncTime(DateTime.UtcNow);

            await Shell.Current.DisplayAlertAsync(
                "Sync Complete",
                $"Found {inserted} new transaction{(inserted == 1 ? "" : "s")}.",
                "OK");
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync(
                "Sync Failed",
                "Something went wrong while syncing. Please try again.",
                "OK");
            // TODO: route ex through whatever logging/telemetry you use elsewhere,
            // once that's wired up — swallowing it silently here otherwise.
        }
        finally
        {
            IsSyncing = false;
        }
    }

    [RelayCommand]
    public async Task ClearAllDataAsync()
    {
        bool confirmed = await Shell.Current.DisplayAlertAsync(
            "Clear All Data",
            "This will permanently delete all your transactions, budgets, and categories. This cannot be undone.",
            "Delete Everything",
            "Cancel");

        if (!confirmed) return;

        await _seeder.ReseedAsync(_databaseService);

        // Restart to onboarding
        if (Application.Current?.Windows.FirstOrDefault() is Window window)
            window.Page = _services.GetRequiredService<WelcomePage>();
    }

    [RelayCommand]
    public async Task OpenExportsAsync() =>
        await Shell.Current.GoToAsync(nameof(ExportPage));

    // ── About ─────────────────────────────────────────────────────────────────
    [RelayCommand]
    public async Task CopyVersionInfoAsync()
    {
        await Clipboard.Default.SetTextAsync(AppVersion);
        await Shell.Current.DisplayAlertAsync("Copied", "Version info copied to clipboard.", "OK");
    }

    [RelayCommand]
    public async Task SendFeedbackAsync()
    {
        //const string email = "feedback@pesascope.app";
        const string email = "booker20dev@gmail.com";
        const string subject = "PesaScope Feedback";

        var body =
            $"\n\n---\n" +
            $"App version: {AppVersion}\n" +
            $"Platform: {DeviceInfo.Platform} {DeviceInfo.VersionString}\n" +
            $"Device: {DeviceInfo.Manufacturer} {DeviceInfo.Model}";

        if (Email.Default.IsComposeSupported)
        {
            try
            {
                var message = new EmailMessage
                {
                    Subject = subject,
                    Body = body,
                    To = [email]
                };
                await Email.Default.ComposeAsync(message);
            }
            catch (FeatureNotSupportedException)
            {
                await FallbackToMailtoAsync(email, subject, body);
            }
        }
        else
        {
            await FallbackToMailtoAsync(email, subject, body);
        }
    }

    private static async Task FallbackToMailtoAsync(string email, string subject, string body)
    {
        var mailto = $"mailto:{email}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";

        try
        {
            await Launcher.Default.OpenAsync(mailto);
        }
        catch
        {
            // No mail handling capability at all on this device — last resort:
            // let the user copy the address manually.
            await Clipboard.Default.SetTextAsync(email);
            await Shell.Current.DisplayAlertAsync(
                "No Email App Found",
                $"We couldn't find an email app on this device. The address {email} has been copied to your clipboard.",
                "OK");
        }
    }

    // ── Developer info ────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task OpenPortfolioAsync() => await OpenLinkAsync(PortfolioUrl);

    [RelayCommand]
    public async Task OpenGitHubAsync() => await OpenLinkAsync(GitHubUrl);

    [RelayCommand]
    public async Task OpenAppGitHubAsync() => await OpenLinkAsync(AppGitHubUrl);

    [RelayCommand]
    public async Task OpenAppWebsiteAsync() => await OpenLinkAsync(AppWebsiteUrl);

    private static async Task OpenLinkAsync(string url)
    {
        try
        {
            await Launcher.Default.OpenAsync(new Uri(url));
        }
        catch
        {
            await Clipboard.Default.SetTextAsync(url);
            await Shell.Current.DisplayAlertAsync(
                "Couldn't Open Link",
                $"The link has been copied to your clipboard instead:\n{url}",
                "OK");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string FormatSyncTime(DateTime utc)
    {
        var local = utc.ToLocalTime();
        var diff = DateTime.Now - local;

        return diff.TotalMinutes < 1 ? "Just now"
             : diff.TotalMinutes < 60 ? $"{(int)diff.TotalMinutes}m ago"
             : diff.TotalHours < 24 ? $"{(int)diff.TotalHours}h ago"
             : local.ToString("MMM d");
    }
}
