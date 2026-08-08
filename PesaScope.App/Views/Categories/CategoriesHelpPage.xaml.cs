namespace PesaScope.App.Views.Categories;

public partial class CategoriesHelpPage : UraniumUI.Pages.UraniumContentPage
{
    public CategoriesHelpPage()
    {
        InitializeComponent();
    }

    private async void OnCloseTapped(object sender, EventArgs e)
    {
        await Shell.Current.Navigation.PopModalAsync();
    }
}