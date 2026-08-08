namespace PesaScope.App.Views.Budgets;

public partial class BudgetsHelpPage : UraniumUI.Pages.UraniumContentPage
{
    public BudgetsHelpPage()
    {
        InitializeComponent();
    }

    private async void OnCloseTapped(object sender, EventArgs e)
    {
        await Shell.Current.Navigation.PopModalAsync();
    }
}