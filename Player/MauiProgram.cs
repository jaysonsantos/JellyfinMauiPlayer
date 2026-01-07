using CommunityToolkit.Maui;
using JellyfinPlayer.Lib;
using JellyfinPlayer.Lib.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mpv.Maui;
using Player.Services;

namespace Player;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMpv()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Add file-based logging (works in both Debug and Release)
        var logDirectory = Path.Combine(FileSystem.AppDataDirectory, "Logs");
        var logFilePath = Path.Combine(logDirectory, $"app-{DateTime.Now:yyyyMMdd}.log");
        builder.Logging.AddProvider(new FileLoggerProvider(logFilePath, LogLevel.Debug));

        // Register Lib services
        builder.Services.AddJellyfinPlayerLib();

        // Register platform-specific secure storage (uses Keychain/Keystore/Credential Manager)
        builder.Services.AddSingleton<ISecureStorageService, PlatformSecureStorageService>();

        // Register SQLite cache service and image cache
        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "JellyfinCache.db");
        builder.Services.AddJellyfinPlayerLibStorage(databasePath);

        // Register application services
        RegisterServices(builder.Services);

        var app = builder.Build();

        // Run database migrations on startup
        InitializeDatabaseAsync(app.Services).GetAwaiter().GetResult();

        return app;
    }

    private static void RegisterServices(IServiceCollection services)
    {
        // Register ViewModels
        services.AddTransient<ViewModels.LoginViewModel>();
        services.AddTransient<ViewModels.HomeViewModel>();
        services.AddTransient<ViewModels.LibraryViewModel>();
        services.AddTransient<ViewModels.ItemDetailViewModel>();
        services.AddTransient<ViewModels.VideoPlayerViewModel>();
        services.AddTransient<ViewModels.LibraryManagementViewModel>();

        // Register Pages
        services.AddTransient<Pages.LoginPage>();
        services.AddTransient<Pages.HomePage>();
        services.AddTransient<Pages.LibraryPage>();
        services.AddTransient<Pages.ItemDetailPage>();
        services.AddTransient<Pages.VideoPlayerPage>();
        services.AddTransient<Pages.LibraryManagementPage>();
    }

    private static async Task InitializeDatabaseAsync(IServiceProvider services)
    {
        try
        {
            // Create a scope to resolve the DbContext
            using var scope = services.CreateScope();
            var dbContextFactory = scope.ServiceProvider.GetService<
                IDbContextFactory<CacheDbContext>
            >();

            if (dbContextFactory is not null)
            {
                await using var dbContext = await dbContextFactory.CreateDbContextAsync();

                // Run any pending migrations
                await dbContext.Database.MigrateAsync();

                // Get logger without type parameter since MauiProgram is static
                var loggerFactory = scope.ServiceProvider.GetService<ILoggerFactory>();
                var logger = loggerFactory?.CreateLogger("DatabaseInitialization");
                logger?.LogInformation("Database migrations completed successfully");
            }
        }
        catch (Exception ex)
        {
            // Log the error but don't crash the app
            var loggerFactory = services.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger("DatabaseInitialization");
            logger?.LogError(ex, "Failed to run database migrations");
        }
    }
}
