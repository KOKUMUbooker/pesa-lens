using PesaScope.App.ViewModels;

namespace PesaScope.App.Views.Budgets;

public partial class BudgetsPage : UraniumUI.Pages.UraniumContentPage
{
    private readonly BudgetsViewModel _vm;
    private bool _loaded;

    public BudgetsPage(BudgetsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded) return;
        _loaded = true;
        await _vm.LoadAsync();
    }
    
    private async void OnHelpTapped(object sender, EventArgs e)
    {
        await Shell.Current.Navigation.PushModalAsync(new BudgetsHelpPage());
    }
}