using JellyfinPlayer.Lib.Storage;

namespace Player;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register detail page routes (LoginPage and HomePage are already in AppShell.xaml)
        Routing.RegisterRoute(Routes.Library, typeof(Pages.LibraryPage));
        Routing.RegisterRoute(Routes.ItemDetail, typeof(Pages.ItemDetailPage));
        Routing.RegisterRoute(Routes.VideoPlayer, typeof(Pages.VideoPlayerPage));
        Routing.RegisterRoute(Routes.MetadataEditor, typeof(Pages.MetadataEditorPage));

        // Check authentication after shell is loaded
        Loaded += OnShellLoaded;
    }

    private async void OnShellLoaded(object? sender, EventArgs e)
    {
        Loaded -= OnShellLoaded;

        // Get secure storage from DI
        var secureStorage = Handler?.MauiContext?.Services.GetService<ISecureStorageService>();
        if (secureStorage is not null)
        {
            await CheckAuthenticationAndSetRoute(secureStorage);
        }
        else
        {
            // Fallback: show login page if we can't check authentication
            CurrentItem = Items.FirstOrDefault(i => i.Route.Contains(Routes.Login)) ?? Items[0];
        }
    }

    private async Task CheckAuthenticationAndSetRoute(ISecureStorageService secureStorage)
    {
        // Check if we have stored credentials
        var accessToken = await secureStorage.GetAsync("jellyfin_access_token");
        var serverUrl = await secureStorage.GetAsync("jellyfin_server_url");

        // Use Shell navigation to navigate to the route; works with registered routes and absolute routing
        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(serverUrl))
            await Shell.Current.GoToAsync($"//{Routes.Login}");
        else
            await Shell.Current.GoToAsync($"//{Routes.Home}");
    }
}
