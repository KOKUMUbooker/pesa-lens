using PesaScope.App.Services.Interfaces;

namespace PesaScope.App.Views.Security;

public partial class AppLockPage : UraniumUI.Pages.UraniumContentPage
{
    private readonly IBiometricAuthService _biometricAuthService;
    private IDispatcherTimer? _clockTimer;

    public AppLockPage(IBiometricAuthService biometricAuthService)
    {
        InitializeComponent();
        _biometricAuthService = biometricAuthService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        StartClock();
        await TryUnlockAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _clockTimer?.Stop();
    }

    private void StartClock()
    {
        UpdateClock(); // immediate update, no 1s blank
        _clockTimer = Dispatcher.CreateTimer();
        _clockTimer.Interval = TimeSpan.FromSeconds(1);
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        TimeLabel.Text = now.ToString("HH:mm");
        DateLabel.Text = now.ToString("dddd, d MMMM");
    }

    private async void OnUnlockClicked(object? sender, EventArgs e)
        => await TryUnlockAsync();

    private async Task TryUnlockAsync()
    {
        var result = await _biometricAuthService.AuthenticateAsync();
        if (result == BiometricCheckResult.Success)
        {
            _clockTimer?.Stop();
            Application.Current!.Windows[0].Page = new AppShell();
            _ = App.TryHandlePendingNavigationAsync(); // flush any pending notification intent
        }
    }
}