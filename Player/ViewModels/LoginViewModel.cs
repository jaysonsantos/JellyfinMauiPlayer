using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JellyfinPlayer.Lib.Models;
using JellyfinPlayer.Lib.Services;
using Microsoft.Extensions.Logging;
using Player.Resources.Strings;

namespace Player.ViewModels;

public sealed partial class LoginViewModel(
    AuthenticationService authenticationService,
    ILogger<LoginViewModel> logger
) : ObservableObject
{
    private const string DefaultServerUrl = "http://localhost:8096";

    [ObservableProperty]
    public partial string ServerUrl { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Username { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial bool IsLoggedIn { get; set; }

    // Event handler removed - using Shell navigation instead

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsLoading)
            return;

        ErrorMessage = null;
        IsLoading = true;

        try
        {
            var result = await authenticationService.AuthenticateAsync(
                ServerUrl,
                Username,
                Password
            );

            if (result is null)
            {
                ErrorMessage = AppResources.LoginPage_InvalidCredentials;
                IsLoading = false;
                return;
            }

            IsLoggedIn = true;

            // Navigate to home page after successful login
            await Shell.Current.GoToAsync($"//{Routes.Home}").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Login failed");
            ErrorMessage = string.Format(AppResources.LoginPage_LoginFailed, ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CheckStoredCredentialsAsync()
    {
        try
        {
            logger.LogInformation("Checking for stored credentials...");

            // Validate stored credentials before auto-login
            var isValid = await authenticationService.ValidateStoredCredentialsAsync();
            if (isValid)
            {
                logger.LogInformation("Stored credentials are valid, navigating to home page");
                IsLoggedIn = true;

                // Navigate to home page after successful login
                await Shell.Current.GoToAsync($"//{Routes.Home}").ConfigureAwait(false);
            }
            else
            {
                logger.LogInformation("No valid stored credentials found, showing login page");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check stored credentials");
        }
    }

    [RelayCommand]
    private async Task LoadStoredLoginCredentialsAsync()
    {
        logger.LogInformation("Initializing login page...");

        var credentials = await authenticationService.GetStoredLoginCredentialsAsync();
        if (credentials is null)
        {
            SetDefaultCredentials();
            return;
        }

        ServerUrl = !string.IsNullOrWhiteSpace(credentials.ServerUrl)
            ? credentials.ServerUrl
            : DefaultServerUrl;
        Username = !string.IsNullOrWhiteSpace(credentials.Username)
            ? credentials.Username
            : string.Empty;
        Password = !string.IsNullOrWhiteSpace(credentials.Password)
            ? credentials.Password
            : string.Empty;

        logger.LogInformation("Login page initialized");
    }

    private void SetDefaultCredentials()
    {
        ServerUrl = DefaultServerUrl;
        Username = string.Empty;
        Password = string.Empty;
    }
}
