using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;

namespace Player.AppiumTests;

/// <summary>
/// Base class for Appium tests that provides common setup and teardown functionality.
/// </summary>
public abstract class AppiumTestBase : IAsyncLifetime
{
    /// <summary>
    /// The Appium driver instance for interacting with the Windows app.
    /// </summary>
    protected WindowsDriver Driver { get; private set; } = null!;

    /// <summary>
    /// Gets the path to the app to launch. Can be an executable path or app ID.
    /// </summary>
    protected abstract string AppPath { get; }

    /// <summary>
    /// Gets the URL of the Appium/WinAppDriver server.
    /// Default is http://127.0.0.1:4723 for local WinAppDriver.
    /// </summary>
    protected virtual string AppiumServerUrl =>
        Environment.GetEnvironmentVariable("APPIUM_SERVER_URL") ?? "http://127.0.0.1:4723";

    /// <summary>
    /// Gets the implicit wait timeout for finding elements.
    /// </summary>
    protected virtual TimeSpan ImplicitWait => TimeSpan.FromSeconds(10);

    public async Task InitializeAsync()
    {
        var appPath = AppPath;

        // Validate app path is set and file exists
        if (string.IsNullOrWhiteSpace(appPath))
        {
            throw new InvalidOperationException(
                "AppPath is null or empty. Ensure PLAYER_APP_PATH environment variable is set."
            );
        }

        var options = new AppiumOptions();
        options.AddAdditionalAppiumOption("app", appPath);
        options.AddAdditionalAppiumOption("platformName", "Windows");
        options.AddAdditionalAppiumOption(
            "deviceName",
            Environment.GetEnvironmentVariable("APPIUM_DEVICE_NAME") ?? "WindowsPC"
        );

        // Create the driver with retry logic for CI environments
        const int maxRetries = 3;
        Exception? lastException = null;

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                Driver = new WindowsDriver(new Uri(AppiumServerUrl), options);
                Driver.Manage().Timeouts().ImplicitWait = ImplicitWait;
                break;
            }
            catch (Exception ex)
            {
                lastException = ex;
                if (i < maxRetries - 1)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
            }
        }

        if (Driver is null && lastException is not null)
        {
            throw new InvalidOperationException(
                $"Failed to connect to Appium server at {AppiumServerUrl} after {maxRetries} attempts. App path was: {appPath}",
                lastException
            );
        }
    }

    public Task DisposeAsync()
    {
        Driver?.Quit();
        Driver?.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Finds an element by its AutomationId (accessibility identifier).
    /// </summary>
    protected AppiumElement FindByAutomationId(string automationId)
    {
        return Driver.FindElement(MobileBy.AccessibilityId(automationId));
    }

    /// <summary>
    /// Finds an element by its name.
    /// </summary>
    protected AppiumElement FindByName(string name)
    {
        return Driver.FindElement(MobileBy.Name(name));
    }

    /// <summary>
    /// Finds an element by XPath.
    /// </summary>
    protected AppiumElement FindByXPath(string xpath)
    {
        return Driver.FindElement(By.XPath(xpath));
    }

    /// <summary>
    /// Waits for an element to be visible with a custom timeout.
    /// </summary>
    protected async Task<AppiumElement?> WaitForElementAsync(
        By by,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default
    )
    {
        timeout ??= ImplicitWait;
        var endTime = DateTime.UtcNow.Add(timeout.Value);

        while (DateTime.UtcNow < endTime)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var element = Driver.FindElement(by);
                if (element is not null && element.Displayed)
                {
                    return element;
                }
            }
            catch (NoSuchElementException)
            {
                // Element not found yet, continue waiting
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }

        return null;
    }
}
