using PesaScope.App.Data;
using PesaScope.App.Data.Repositories;
using PesaScope.App.Data.Repositories.Interfaces;
using PesaScope.App.Views.Onboarding;
using PesaScope.App.Views.Security;
using PesaScope.App.Views.Transactions;
using Plugin.LocalNotification;
using Plugin.LocalNotification.EventArgs;

namespace PesaScope.App;

public partial class App : Application
{
    private readonly IAppSettingsRepository _appSettingsRepo;
    private readonly IServiceProvider _services;
    private static string? _pendingMpesaCode;
    private static bool _pendingBudgetTap;

    // Signals when DB init + seeding are done
    private readonly TaskCompletionSource _dbReady = new();

    public App(
        DatabaseService databaseService,
        DatabaseSeeder seeder,
        IAppSettingsRepository appSettingsRepo,
        IServiceProvider services)
    {
        InitializeComponent();

        _appSettingsRepo = appSettingsRepo;
        _services = services;

        // Subscribe to notification tap event
        LocalNotificationCenter.Current.NotificationActionTapped += OnNotificationActionTapped;

        // Kick off init — when done, signal _dbReady
        _ = InitializeAsync(databaseService, seeder);
        _services = services;
    }

    private async Task InitializeAsync(DatabaseService databaseService, DatabaseSeeder seeder)
    {
        try
        {
            await databaseService.InitializeAsync();
            await seeder.SeedAsync();
            _dbReady.TrySetResult();
        }
        catch (Exception ex)
        {
            _dbReady.TrySetException(ex);
        }
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Return a blank window immediately so the splash screen can dismiss.
        // Once the DB is ready, swap the window's page to the correct start page.
        var window = new Window(new ContentPage()); // blank placeholder

        _ = SetStartPageAsync(window);

        return window;
    }

    private async Task SetStartPageAsync(Window window)
    {
        try
        {
            // Wait for DB init + seeding to finish before reading settings
            await _dbReady.Task;

            var settings = await _appSettingsRepo.GetAsync();

            // Apply saved theme before showing any UI
            Application.Current!.UserAppTheme = settings.Theme switch
            {
                PesaScope.Core.Models.AppTheme.Light => Microsoft.Maui.ApplicationModel.AppTheme.Light,
                PesaScope.Core.Models.AppTheme.Dark => Microsoft.Maui.ApplicationModel.AppTheme.Dark,
                _ => Microsoft.Maui.ApplicationModel.AppTheme.Unspecified
            };

            Page startPage;

            if (settings.ImportComplete && !settings.OnboardingComplete)
                startPage = _services.GetRequiredService<ImportProgressPage>();
            else if (!settings.OnboardingComplete)
                startPage = _services.GetRequiredService<WelcomePage>();
            else if (settings.AppLockEnabled)
                startPage = _services.GetRequiredService<AppLockPage>();
            else
                startPage = new AppShell();

            // Switch to the real page on the UI thread
            window.Page = startPage;
        }
        catch (Exception ex)
        {
            // Surface the error visibly rather than hanging on a blank screen
            window.Page = new ContentPage
            {
                Content = new Label
                {
                    Text = $"Startup error:\n\n{ex.Message}",
                    TextColor = Colors.Red,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    Margin = new Thickness(24)
                }
            };
        }
    }

    static void OnNotificationActionTapped(NotificationActionEventArgs e)
    {
        if (!e.IsTapped) return;

        var mpesaCode = e.Request.ReturningData;
        if (!string.IsNullOrWhiteSpace(mpesaCode))
            _pendingMpesaCode = mpesaCode;
        else
            _pendingBudgetTap = true;

        _ = TryHandlePendingNavigationAsync();
    }

    // Called both from the notification tap AND from AppLockPage after a
    // successful unlock, so the pending intent is honored whenever it becomes possible.
    public static async Task TryHandlePendingNavigationAsync()
    {
        if (_pendingMpesaCode is null && !_pendingBudgetTap) return;

        // Wait for Shell to exist — but don't give up; this may legitimately
        // take a while if the app is sitting on the lock screen.
        for (int i = 0; i < 100 && Shell.Current is null; i++)
            await Task.Delay(100);

        if (Shell.Current is null) return; // still nothing after 10s — give up quietly

        try
        {
            if (_pendingMpesaCode is not null)
            {
                var code = _pendingMpesaCode;
                _pendingMpesaCode = null;
                await Shell.Current.GoToAsync($"{nameof(TransactionDetailPage)}?code={code}");
            }
            else if (_pendingBudgetTap)
            {
                _pendingBudgetTap = false;
                await Shell.Current.GoToAsync("//Budgets/BudgetsPage");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Notification nav] {ex.Message}");
        }
    }
}

// <summary>
// App constructor
//  └── fires InitializeAsync(fire-and-forget, sets _dbReady when done)

// CreateWindow(called almost immediately by MAUI)
//  └── returns Window(blank page) instantly — splash screen dismisses
//  └── fires SetStartPageAsync

// SetStartPageAsync
//  └── awaits _dbReady.Task  ← waits here until InitializeAsync completes
//  └── reads settings
//  └── sets window.Page = WelcomePage / AppLockPage / AppShell
// </summary>