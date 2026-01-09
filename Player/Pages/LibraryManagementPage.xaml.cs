using Player.ViewModels;

namespace Player.Pages;

public partial class LibraryManagementPage : ContentPage
{
    private readonly LibraryManagementViewModel _viewModel;

    public LibraryManagementPage(LibraryManagementViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadDataCommand.Execute(null);
    }
}
