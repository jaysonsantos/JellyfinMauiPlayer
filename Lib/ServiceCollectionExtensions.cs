using JellyfinPlayer.Lib.Api;
using JellyfinPlayer.Lib.Services;
using JellyfinPlayer.Lib.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JellyfinPlayer.Lib;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddJellyfinPlayerLib()
        {
            // HTTP Client
            services.AddHttpClient(
                "Jellyfin",
                client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                }
            );

            // Services
            services.AddScoped<DeviceInfoService>();
            services.AddScoped<AuthenticationService>();
            services.AddScoped<MediaService>();
            services.AddScoped<IPlaybackService, PlaybackService>();
            services.AddScoped<IMetadataService, MetadataService>();
            services.AddScoped<ILibraryManagementService, LibraryManagementService>();
            services.AddSingleton<JellyfinApiClientFactory>();
            services.AddSingleton<RetryPolicy>();

            // Image Cache Service - cacheDirectory should be provided by platform-specific code
            // services.AddSingleton<IImageCacheService>(sp =>
            // {
            //     var cacheDir = Path.Combine(FileSystem.AppDataDirectory, "ImageCache");
            //     return new ImageCacheService(
            //         sp.GetRequiredService<JellyfinApiClientFactory>(),
            //         cacheDir,
            //         sp.GetRequiredService<ILogger<ImageCacheService>>());
            // });

            // Storage - Platform implementations will be registered in Player project
            // services.AddScoped<IStorageService, SqliteStorageService>();
            // services.AddScoped<ISecureStorageService, PlatformSecureStorageService>();

            // Entity Framework Core - Cache database
            // Note: dbPath should be provided by platform-specific code in Player project
            // services.AddDbContextFactory<CacheDbContext>(options =>
            //     options.UseSqlite($"Data Source={dbPath}"));
            // services.AddScoped<IStorageService, SqliteCacheService>();

            return services;
        }

        public IServiceCollection AddJellyfinPlayerLibStorage(string databasePath)
        {
            services.AddDbContextFactory<CacheDbContext>(options =>
                options.UseSqlite($"Data Source={databasePath}")
            );
            services.AddScoped<IStorageService, SqliteCacheService>();

            // Register Image Cache Service
            services.AddSingleton<IImageCacheService>(sp =>
            {
                var cacheDir = Path.Combine(
                    Path.GetDirectoryName(databasePath) ?? string.Empty,
                    "ImageCache"
                );
                return new ImageCacheService(
                    cacheDir,
                    sp.GetRequiredService<ILogger<ImageCacheService>>()
                );
            });

            return services;
        }
    }
}
