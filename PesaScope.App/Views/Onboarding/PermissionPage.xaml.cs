using Android.App.Roles;
using PesaScope.App.Platforms.Android;

namespace PesaScope.App.Views.Onboarding;

public partial class PermissionPage : UraniumUI.Pages.UraniumContentPage
{
    private readonly IServiceProvider _services;

    private enum Step { ReceiveSms, DefaultApp }
    private Step _currentStep = Step.ReceiveSms;

    public PermissionPage(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Subscribe each time page appears; unsubscribe on disappear
        SmsRoleActivityResultHelper.RoleRequestCompleted += OnRoleRequestCompleted;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        SmsRoleActivityResultHelper.RoleRequestCompleted -= OnRoleRequestCompleted;
    }

    // ── Primary button ────────────────────────────────────────────────────────

    private async void OnPrimaryButtonClicked(object? sender, EventArgs e)
    {
        PrimaryButton.IsEnabled = false;
        DeniedBanner.IsVisible = false;

        var stepAtClick = _currentStep; // capture before any await

        if (_currentStep == Step.ReceiveSms)
            await HandleReceiveSmsStepAsync();
        else
            HandleDefaultAppStep();

        // Re-enable only for Step 1 — Step 2 re-enables via OnRoleRequestCompleted
        if (stepAtClick == Step.ReceiveSms)
            PrimaryButton.IsEnabled = true;
    }

    // ── Step 1: RECEIVE_SMS runtime permission ────────────────────────────────

    private async Task HandleReceiveSmsStepAsync()
    {
        var status = await RequestReceiveSmsPermissionAsync();

        if (status == PermissionStatus.Granted)
        {
            AdvanceToStep2();
        }
        else
        {
            ShowDeniedBanner(
                title: "Permission denied",
                message: "PesaScope cannot capture new M-Pesa transactions without this. " +
                         "Tap Try Again or go to Settings -> Apps -> PesaScope -> Permissions -> SMS.");
            PrimaryButton.Text = "Try Again";
        }
    }

    private static async Task<PermissionStatus> RequestReceiveSmsPermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Sms>();
        return status == PermissionStatus.Granted
            ? status
            : await Permissions.RequestAsync<Permissions.Sms>();
    }

    // ── Step 2: Temporary default SMS app (RoleManager, API 29+) ─────────────

    private void HandleDefaultAppStep()
    {
        Console.WriteLine("In HandleDefaultAppStep , _currentStep : " + _currentStep);
        var activity = Platform.CurrentActivity
            ?? throw new InvalidOperationException("No current activity");

        var roleManager = activity.GetSystemService(Android.Content.Context.RoleService)
            as RoleManager;

        if (roleManager == null)
        {
            // Device doesn't support RoleManager — very unlikely on API 29+
            ShowDeniedBanner(
                title: "Not supported",
                message: "Your device doesn't support this feature. Tap Skip to continue.");
            PrimaryButton.IsEnabled = true;
            return;
        }

        if (roleManager.IsRoleHeld(RoleManager.RoleSms))
        {
            // Already the default — go straight to import
            NavigateToImportProgress(historicalImportEnabled: true);
            return;
        }

        // Fire the system role-request dialog.
        // Result comes back via MainActivity.OnActivityResult
        // -> SmsRoleActivityResultHelper.NotifyResult
        // -> OnRoleRequestCompleted (subscribed above)
        var intent = roleManager.CreateRequestRoleIntent(RoleManager.RoleSms);
        activity.StartActivityForResult(intent, SmsRoleActivityResultHelper.RequestCode);

        // PrimaryButton stays disabled until OnRoleRequestCompleted fires
    }

    private void OnRoleRequestCompleted(bool granted)
    {
        // This arrives on a background thread — marshal to UI thread
        MainThread.BeginInvokeOnMainThread(() =>
        {
            PrimaryButton.IsEnabled = true;

            if (granted)
            {
                NavigateToImportProgress(historicalImportEnabled: true);
            }
            else
            {
                ShowDeniedBanner(
                    title: "Default app not set",
                    message: "Without this, PesaScope can only capture future transactions. " +
                             "Tap 'Set as Default' to try again, or skip to continue without history.");
                PrimaryButton.Text = "Set as Default";
            }
        });
    }

    // ── Skip ──────────────────────────────────────────────────────────────────

    private void OnSkipImportTapped(object? sender, EventArgs e) =>
        NavigateToImportProgress(historicalImportEnabled: false);

    // ── Navigation ────────────────────────────────────────────────────────────

    private void NavigateToImportProgress(bool historicalImportEnabled)
    {
        var importPage = _services.GetRequiredService<ImportProgressPage>();
        importPage.HistoricalImportEnabled = historicalImportEnabled;

        if (Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault() is Window window)
            window.Page = importPage;
    }

    // ── UI helpers ────────────────────────────────────────────────────────────

    private void AdvanceToStep2()
    {
        _currentStep = Step.DefaultApp;

        Step2Dot.BackgroundColor =
            (Color)Microsoft.Maui.Controls.Application.Current!.Resources["Primary"];
        Step2Label.TextColor = Colors.White;

        Step1View.IsVisible = false;
        Step2View.IsVisible = true;
        DeniedBanner.IsVisible = false;

        PrimaryButton.Text = "Set as Default SMS App";
    }

    private void ShowDeniedBanner(string title, string message)
    {
        DeniedTitle.Text = title;
        DeniedMessage.Text = message;
        DeniedBanner.IsVisible = true;
    }
}