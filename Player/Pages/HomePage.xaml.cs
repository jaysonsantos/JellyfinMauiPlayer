using Player.ViewModels;

namespace Player.Pages;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _viewModel;

    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Load data when page appears if not already loaded
        if (_viewModel.Libraries.Count == 0 && !_viewModel.IsLoading)
        {
            _viewModel.LoadDataCommand.Execute(null);
        }
    }
}
