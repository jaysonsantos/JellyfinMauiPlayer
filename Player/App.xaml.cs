using Player.Services;

namespace Player;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Load the correct theme based on system theme
        LoadThemeForCurrentMode();

        // Listen for theme changes
        RequestedThemeChanged += OnRequestedThemeChanged;
    }

    private void LoadThemeForCurrentMode()
    {
        ResourceDictionary themeToLoad =
            RequestedTheme == AppTheme.Dark
                ? new Resources.Themes.DarkTheme()
                : new Resources.Themes.LightTheme();

        // Remove old theme if exists
        ResourceDictionary? oldTheme = Resources.MergedDictionaries.FirstOrDefault(d =>
            d.GetType() == typeof(Resources.Themes.LightTheme)
            || d.GetType() == typeof(Resources.Themes.DarkTheme)
        );

        if (oldTheme is not null)
        {
            Resources.MergedDictionaries.Remove(oldTheme);
        }

        // Add new theme
        Resources.MergedDictionaries.Add(themeToLoad);
    }

    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        LoadThemeForCurrentMode();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // AppShell will be created by MAUI framework
        // The dependency injection happens in the constructor
        var window = new Window(new AppShell());

        return window;
    }
}
