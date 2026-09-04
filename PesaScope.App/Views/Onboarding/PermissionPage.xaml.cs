namespace PesaScope.App.Views.Onboarding;

public partial class PermissionPage : UraniumUI.Pages.UraniumContentPage
{
    private readonly IServiceProvider _services;

    public PermissionPage(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
    }

    private async void OnPrimaryButtonClicked(object? sender, EventArgs e)
    {
        PrimaryButton.IsEnabled = false;
        DeniedBanner.IsVisible = false;

        // READ_SMS covers historical import; RECEIVE_SMS covers live capture
        // via MpesaSmsReceiver. Neither requires default-handler status.
        var readStatus = await RequestPermissionAsync<Permissions.Sms>();

        if (readStatus == PermissionStatus.Granted)
        {
            NavigateToImportProgress();
        }
        else
        {
            ShowDeniedBanner(
                title: "Permission denied",
                message: "PesaScope cannot read or capture M-Pesa transactions without this. " +
                         "Tap Try Again or go to Settings -> Apps -> PesaScope -> Permissions -> SMS.");
            PrimaryButton.Text = "Try Again";
        }

        PrimaryButton.IsEnabled = true;
    }

    private static async Task<PermissionStatus> RequestPermissionAsync<T>() where T : Permissions.BasePermission, new()
    {
        var status = await Permissions.CheckStatusAsync<T>();
        return status == PermissionStatus.Granted
            ? status
            : await Permissions.RequestAsync<T>();
    }

    private void NavigateToImportProgress()
    {
        var importPage = _services.GetRequiredService<ImportProgressPage>();

        if (Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault() is Window window)
            window.Page = importPage;
    }

    private void ShowDeniedBanner(string title, string message)
    {
        DeniedTitle.Text = title;
        DeniedMessage.Text = message;
        DeniedBanner.IsVisible = true;
    }
}