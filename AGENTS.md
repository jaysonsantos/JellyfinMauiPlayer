# AGENT GUIDANCE

1. Restore workloads first: `dotnet workload restore` (root).
2. Build everything with `dotnet build JellyfinPlayer.slnx -warnaserror`.
3. Player-only run/build: `dotnet build Player/Player.csproj -t:Run`.
4. **Testing:**
   - Run `Lib.Tests`: `dotnet test Lib.Tests -f net10.0`
   - Run `Mpv.Sys.Tests`: `dotnet test Mpv.Sys.Tests -f net10.0`
   - Run single test: `dotnet test <project> -f net10.0 --filter FullyQualifiedName~TestName`
   - **Important:** Always use `-f net10.0` flag when running tests as these projects multi-target.
   - Changes to `Lib` should include corresponding tests in `Lib.Tests`.
   - Changes to `Mpv.Sys` should include corresponding tests in `Mpv.Sys.Tests`.
5. Regenerate Kiota client using `make Lib/Generated` (needs `kiota generate`).
6. Format C# via `dotnet tool restore` then `dotnet csharpier .`; no manual tweaks.
7. `.editorconfig` rules apply: 4-space indent, insert final newline disabled, sorted/System-first usings.
8. Keep namespaces file-scoped and lean on `GlobalUsings.cs` for common imports.
9. Follow PRD separation: Lib = business logic; Player hosts all UI and ViewModels.
10. Prefer `sealed`/`record` types, target-typed `new`, collection expressions, nullable enabled.
11. Use explicit types over `var` unless analyzer suggests otherwise.
12. Naming: PascalCase for types/members, camelCase locals/parameters, `_camelCase` private fields.
13. `[ObservableProperty]` from CommunityToolkit requires the containing class marked `partial`.
14. Inject dependencies via primary constructors (e.g., `public sealed class MacKeyboardHandler(ILogger<MacKeyboardHandler> logger)`); avoid service locator patterns.
15. Every async API accepts `CancellationToken` and uses `await` (no blocking `.Result`).
16. Guard inputs with `ArgumentNullException.ThrowIfNull`/`ThrowIfNullOrWhiteSpace`; prefer `ServiceResult` for recoverable errors.
17. Return `IReadOnlyList<T>`/`IEnumerable<T>` from public APIs to avoid mutable exposure.
18. Use structured logging through `ILogger<T>`; no direct `Console` usage.
19. Cross-platform resources/text live in `Resources/Strings`; update `AppResources.resx` and related designer plus note roadmap shifts in `PRD.md`/`TODO.md`.
20. **Short-Circuit Ifs (Early Returns):**
    - Use early returns (guard clauses) to reduce nesting and improve readability.
    - Check for invalid conditions first and return/exit early rather than wrapping valid logic in nested if blocks.
    - Example:
      ```csharp
      // Bad: Nested if
      public void Process(object? data)
      {
          if (data is MyType myData)
          {
              DoWork(myData);
          }
      }
      
      // Good: Early return
      public void Process(object? data)
      {
          if (data is not MyType myData)
              return;
          
          DoWork(myData);
      }
      ```
    - This pattern keeps the main logic at the lowest indentation level and makes the function flow clearer.
21. **Analyzer Compliance (Zero Warnings Policy):**
    - Use `string.Equals(a, b, StringComparison.Ordinal)` instead of `==` for string comparisons (MA0006).
    - Remove unused constructor parameters; if needed for future use, suppress with `#pragma warning disable CS9113` and add a comment explaining why.
    - Keep methods under 60 lines (MA0051); extract helper methods if exceeding this limit.
    - Ensure nullability compatibility: when using generic constraints like `where T : class`, ensure lambdas return non-nullable types or use null-coalescing operators.
    - Fix all warnings before committing; build with `-warnaserror` ensures warnings are treated as errors.
22. **Shell Navigation & Routes:**
    - All route keys MUST be defined in `Player/Routes.cs` as string constants.
    - Register routes programmatically in `AppShell.xaml.cs` using `Routing.RegisterRoute(Routes.PageName, typeof(Pages.PageName))`.
    - Only add pages to `AppShell.xaml` as `<ShellContent>` if they are top-level navigation items (tabs, flyout menu).
    - Detail pages navigated via `GoToAsync()` should ONLY be registered as routes, NOT added to `AppShell.xaml` to avoid duplicate route errors.
    - Use absolute routing with `//` prefix for root-level navigation: `Shell.Current.GoToAsync($"//{Routes.Login}")`.
    - Use relative routing without `//` for hierarchical navigation: `Shell.Current.GoToAsync(Routes.ItemDetail, parameters)`.
23. **XAML Pages & Data Binding:**
    - Always specify `x:DataType` on `ContentPage` for the page's ViewModel: `x:DataType="viewmodels:PageViewModel"`.
    - Always specify `x:DataType` on `DataTemplate` elements in CollectionView/ListView for compile-time binding verification.
    - Import model namespaces in XAML headers: `xmlns:models="using:JellyfinPlayer.Lib.Models"`.
    - Use typed DataTemplates: `<DataTemplate x:DataType="{x:Type models:MediaItem}">` instead of untyped `<DataTemplate>`.
    - This enables compile-time binding checks, IntelliSense, and better performance with compiled bindings.
24. **Theme-Aware Colors (Microsoft MAUI Theming Pattern):**
    - ALWAYS use `{DynamicResource}` for theme colors to enable runtime theme switching.
    - NEVER hardcode colors in XAML (e.g., `TextColor="Red"`, `BackgroundColor="White"`).
    - NEVER use `{AppThemeBinding}` or `{StaticResource}` for theme colors (they won't update when themes change).
    - Theme resource keys are defined in both `Player/Resources/Themes/LightTheme.xaml` and `Player/Resources/Themes/DarkTheme.xaml`.
    - Common theme keys:
      - Primary text: `{DynamicResource TextColor}`
      - Muted/secondary text: `{DynamicResource MutedTextColor}`
      - Error messages: `{DynamicResource ErrorColor}`
      - Page backgrounds: `{DynamicResource PageBackgroundColor}`
      - Primary UI elements: `{DynamicResource PrimaryColor}`
      - Video player text: `{DynamicResource VideoTextColor}`
    - Both theme files define the SAME keys with different values for light/dark themes.
    - Static colors (like Gray100, Gray600) in `Player/Resources/Styles/Colors.xaml` are base colors shared by both themes and can use `{StaticResource}`.
    - See `Player/THEMING.md` for complete theming documentation, available theme keys, and how to switch themes at runtime.
24. **Theme-Aware Colors (Microsoft MAUI Theming Pattern):**
    - ALWAYS use `{DynamicResource}` for theme colors to enable runtime theme switching.
    - NEVER hardcode colors in XAML (e.g., `TextColor="Red"`, `BackgroundColor="White"`).
    - NEVER use `{AppThemeBinding}` or `{StaticResource}` for theme colors (they won't update when themes change).
    - Theme resource keys are defined in both `Player/Resources/Themes/LightTheme.xaml` and `Player/Resources/Themes/DarkTheme.xaml`.
    - Common theme keys:
      - Primary text: `{DynamicResource TextColor}`
      - Muted/secondary text: `{DynamicResource MutedTextColor}`
      - Error messages: `{DynamicResource ErrorColor}`
      - Page backgrounds: `{DynamicResource PageBackgroundColor}`
      - Primary UI elements: `{DynamicResource PrimaryColor}`
      - Video player text: `{DynamicResource VideoTextColor}`
    - Both theme files define the SAME keys with different values for light/dark themes.
    - Static colors (like Gray100, Gray600) in `Player/Resources/Styles/Colors.xaml` are base colors shared by both themes and can use `{StaticResource}`.
    - See `Player/THEMING.md` for complete theming documentation, available theme keys, and how to switch themes at runtime.
25. **UI Thread and ConfigureAwait:**
    - All UIKit/MAUI UI operations (Shell navigation, updating UI elements, etc.) MUST run on the main/UI thread.
    - In ViewModels or async methods that interact with UI, avoid `.ConfigureAwait(false)` before UI operations to ensure you stay on the UI thread.
    - Only use `.ConfigureAwait(false)` when you don't need to return to the UI thread for subsequent operations.
    - If you must use `.ConfigureAwait(false)` and then perform UI operations, explicitly dispatch to the UI thread using `MainThread.InvokeOnMainThreadAsync()`.
    - Common UI operations that require UI thread: `Shell.Current.GoToAsync()`, setting observable properties bound to UI, `Application.Current.MainPage` access.
    - Failure to follow this will result in `UIKit.UIKitThreadAccessException` on iOS/Mac or similar exceptions on other platforms.
26. **Third-Party Dependencies:**
    - mpv headers and documentation are located in `ThirdParty/mpv/`:
      - Headers: `ThirdParty/mpv/include/` contains `client.h`, `render.h`, `render_gl.h`, `stream_cb.h`
      - Documentation: `ThirdParty/mpv/docs/` contains `input.rst` (commands like loadfile), `options.rst`, `lua.rst`, `client-api-changes.rst`
      - Source: https://github.com/mpv-player/mpv
    - When implementing mpv integration, refer to the headers and command documentation in `ThirdParty/mpv/`.
27. **Event Handlers and EventArgs:**
    - All event handlers MUST follow the standard .NET pattern: `EventHandler<TEventArgs>` where `TEventArgs` derives from `System.EventArgs`.
    - NEVER use `EventHandler<int>`, `EventHandler<string>`, or other primitive types directly as the generic parameter.
    - Create a custom EventArgs class (e.g., `TrackChangeEventArgs`, `TrackSelectedEventArgs`) that derives from `EventArgs` to hold the data.
    - Example pattern:
      ```csharp
      public sealed class TrackChangeEventArgs(int trackId) : EventArgs
      {
          public int TrackId { get; } = trackId;
      }
      public event EventHandler<TrackChangeEventArgs>? AudioTrackChanged;
      // Usage: AudioTrackChanged?.Invoke(this, new TrackChangeEventArgs(trackId));
      ```
    - See existing examples: `VideoPositionEventArgs`, `TrackChangeEventArgs`, `TrackSelectedEventArgs`.
