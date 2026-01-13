namespace Player.AppiumTests;

/// <summary>
/// Tests for the LoginPage to verify it renders correctly.
/// </summary>
public sealed class LoginPageTests : AppiumTestBase
{
    /// <summary>
    /// Gets the path to the Player app executable.
    /// This can be configured via the PLAYER_APP_PATH environment variable.
    /// </summary>
    protected override string AppPath =>
        Environment.GetEnvironmentVariable("PLAYER_APP_PATH")
        ?? throw new InvalidOperationException(
            "PLAYER_APP_PATH environment variable must be set to the path of the Player.exe"
        );

    /// <summary>
    /// Verifies that the login page renders with the expected title.
    /// </summary>
    [Fact]
    public void LoginPage_ShouldRenderWithTitle()
    {
        // The app should start on the login page
        // Look for the app title "Jellyfin Player"
        var appTitleElement = FindByName("Jellyfin Player");

        Assert.NotNull(appTitleElement);
        Assert.True(appTitleElement.Displayed, "App title should be visible");
    }

    /// <summary>
    /// Verifies that the login page contains the server URL input field.
    /// </summary>
    [Fact]
    public void LoginPage_ShouldHaveServerUrlField()
    {
        // Look for the "Server URL" label
        var serverUrlLabel = FindByName("Server URL");

        Assert.NotNull(serverUrlLabel);
        Assert.True(serverUrlLabel.Displayed, "Server URL label should be visible");
    }

    /// <summary>
    /// Verifies that the login page contains the username input field.
    /// </summary>
    [Fact]
    public void LoginPage_ShouldHaveUsernameField()
    {
        // Look for the "Username" label
        var usernameLabel = FindByName("Username");

        Assert.NotNull(usernameLabel);
        Assert.True(usernameLabel.Displayed, "Username label should be visible");
    }

    /// <summary>
    /// Verifies that the login page contains the password input field.
    /// </summary>
    [Fact]
    public void LoginPage_ShouldHavePasswordField()
    {
        // Look for the "Password" label
        var passwordLabel = FindByName("Password");

        Assert.NotNull(passwordLabel);
        Assert.True(passwordLabel.Displayed, "Password label should be visible");
    }

    /// <summary>
    /// Verifies that the login page contains the login button.
    /// </summary>
    [Fact]
    public void LoginPage_ShouldHaveLoginButton()
    {
        // Look for the "Login" button
        var loginButton = FindByName("Login");

        Assert.NotNull(loginButton);
        Assert.True(loginButton.Displayed, "Login button should be visible");
    }

    /// <summary>
    /// Verifies that all essential login page elements are present.
    /// This is a comprehensive test that checks all main UI elements in one test.
    /// </summary>
    [Fact]
    public void LoginPage_ShouldRenderAllEssentialElements()
    {
        // Verify the main title
        var appTitle = FindByName("Jellyfin Player");
        Assert.NotNull(appTitle);

        // Verify the subtitle
        var subtitle = FindByName("Connect to your Jellyfin server");
        Assert.NotNull(subtitle);

        // Verify form labels
        var serverUrlLabel = FindByName("Server URL");
        Assert.NotNull(serverUrlLabel);

        var usernameLabel = FindByName("Username");
        Assert.NotNull(usernameLabel);

        var passwordLabel = FindByName("Password");
        Assert.NotNull(passwordLabel);

        // Verify the login button
        var loginButton = FindByName("Login");
        Assert.NotNull(loginButton);
    }
}
