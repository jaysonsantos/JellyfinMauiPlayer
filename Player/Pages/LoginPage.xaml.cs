using Player.ViewModels;

namespace Player.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        // Check for stored credentials when page loads
        Loaded += async (s, e) => await viewModel.CheckStoredCredentialsCommand.ExecuteAsync(null);
    }
}
