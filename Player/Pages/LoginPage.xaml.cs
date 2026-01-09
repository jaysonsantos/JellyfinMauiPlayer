using Player.ViewModels;

namespace Player.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        // Load stored credentials and check for valid session when page loads
        Loaded += async (s, e) =>
        {
            await viewModel.LoadStoredLoginCredentialsCommand.ExecuteAsync(null);
            await viewModel.CheckStoredCredentialsCommand.ExecuteAsync(null);
        };
    }
}
