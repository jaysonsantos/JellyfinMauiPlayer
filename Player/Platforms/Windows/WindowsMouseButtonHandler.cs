using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Player.Platforms.Windows;

/// <summary>
/// Handles Windows-specific mouse button events (XButton1/XButton2) for navigation.
/// </summary>
public sealed class WindowsMouseButtonHandler
{
    private readonly ILogger<WindowsMouseButtonHandler> _logger;
    private IntPtr _originalWndProc;
    private IntPtr _hwnd;
    private WndProcDelegate? _wndProcDelegate;
    private Func<Task>? _backNavigationCallback;

    // Windows message constants
    private const int WM_XBUTTONDOWN = 0x020B;
    private const int XBUTTON1 = 0x0001; // Back button
    private const int XBUTTON2 = 0x0002; // Forward button
    private const int GWLP_WNDPROC = -4;

    // Windows API imports
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallWindowProc(
        IntPtr lpPrevWndFunc,
        IntPtr hWnd,
        uint Msg,
        IntPtr wParam,
        IntPtr lParam
    );

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    public WindowsMouseButtonHandler(ILogger<WindowsMouseButtonHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Hooks into the Windows message pump to handle mouse button events.
    /// </summary>
    public void Initialize(Microsoft.UI.Xaml.Window window, Func<Task> backNavigationCallback)
    {
        try
        {
            _backNavigationCallback = backNavigationCallback;
            _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

            // Create delegate for our WndProc
            _wndProcDelegate = new WndProcDelegate(WndProc);

            // Subclass the window by replacing the window procedure
            _originalWndProc = SetWindowLongPtr(
                _hwnd,
                GWLP_WNDPROC,
                Marshal.GetFunctionPointerForDelegate(_wndProcDelegate)
            );

            if (_originalWndProc == IntPtr.Zero)
            {
                _logger.LogWarning("Failed to subclass window for mouse button handling");
                return;
            }

            _logger.LogInformation("Windows mouse button handler initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Windows mouse button handler");
        }
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        // Handle XButton messages
        if (msg == WM_XBUTTONDOWN)
        {
            int button = (int)((wParam.ToInt64() >> 16) & 0xFFFF);

            if (button == XBUTTON1 && _backNavigationCallback is not null)
            {
                // Back button clicked - invoke on main thread
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        await _backNavigationCallback();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during mouse button navigation");
                    }
                });

                // Return 1 to indicate we handled the message
                return new IntPtr(1);
            }
            // XBUTTON2 could be used for forward navigation in the future
        }

        // Call the original window procedure for all other messages
        return CallWindowProc(_originalWndProc, hWnd, msg, wParam, lParam);
    }
}
