using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using WinUIKeyboardAccelerator = Microsoft.UI.Xaml.Input.KeyboardAccelerator;
using WinUIKeyboardAcceleratorInvokedEventArgs = Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs;

namespace Player.Platforms.Windows;

/// <summary>
/// Handles Windows-specific keyboard shortcuts (Escape, Backspace) for navigation.
/// </summary>
public sealed class WindowsKeyboardHandler
{
    private readonly ILogger<WindowsKeyboardHandler> _logger;
    private Func<Task>? _backNavigationCallback;

    public WindowsKeyboardHandler(ILogger<WindowsKeyboardHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Configures keyboard accelerators for Escape and Backspace keys.
    /// </summary>
    public void Initialize(Microsoft.UI.Xaml.Window window, Func<Task> backNavigationCallback)
    {
        try
        {
            _backNavigationCallback = backNavigationCallback;

            // Escape key accelerator
            var escapeAccelerator = new WinUIKeyboardAccelerator
            {
                Key = global::Windows.System.VirtualKey.Escape,
                IsEnabled = true,
            };
            escapeAccelerator.Invoked += OnKeyboardAcceleratorInvoked;

            // Backspace key accelerator
            var backspaceAccelerator = new WinUIKeyboardAccelerator
            {
                Key = global::Windows.System.VirtualKey.Back,
                IsEnabled = true,
            };
            backspaceAccelerator.Invoked += OnKeyboardAcceleratorInvoked;

            // Add accelerators to the window content
            if (window.Content is UIElement content)
            {
                content.KeyboardAccelerators.Add(escapeAccelerator);
                content.KeyboardAccelerators.Add(backspaceAccelerator);
                _logger.LogInformation(
                    "Windows keyboard accelerators configured (Escape, Backspace)"
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure Windows keyboard accelerators");
        }
    }

    private void OnKeyboardAcceleratorInvoked(
        WinUIKeyboardAccelerator sender,
        WinUIKeyboardAcceleratorInvokedEventArgs args
    )
    {
        if (_backNavigationCallback is not null)
        {
            args.Handled = true;
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await _backNavigationCallback();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during keyboard shortcut navigation");
                }
            });
        }
    }
}
