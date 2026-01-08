using Microsoft.Extensions.Logging;
using Mpv.Maui.Controls;
using Player.ViewModels;

namespace Player.Pages;

public sealed partial class VideoPlayerPage : ContentPage, IQueryAttributable, IDisposable
{
    private readonly VideoPlayerViewModel _viewModel;
    private readonly ILogger<VideoPlayerPage> _logger;
    private IDispatcherTimer? _hideControlsTimer;
    private bool _controlsVisible = true;
    private bool _disposed;
    private bool _isSliderDragging;
    private DisplayOrientation _previousOrientation;

    private const uint FadeAnimationDuration = 250;
    private const int AutoHideDelayMs = 4000;

    public VideoPlayerPage(VideoPlayerViewModel viewModel, ILogger<VideoPlayerPage> logger)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _logger = logger;
        BindingContext = viewModel;

        InitializeAutoHideTimer();
        SubscribeToVideoEvents();

        // Wire up the seek command directly
        PositionSlider.SeekCommand = MpvElement.SeekCommand;

        // Track slider drag state
        PositionSlider.DragStarted += OnSliderDragStarted;
        PositionSlider.DragCompleted += OnSliderDragCompleted;

        // Subscribe to orientation changes
        DeviceDisplay.MainDisplayInfoChanged += OnDisplayInfoChanged;
        _previousOrientation = DeviceDisplay.MainDisplayInfo.Orientation;

        // Subscribe to track selection events
        _viewModel.AudioTrackSelected += OnAudioTrackSelected;
        _viewModel.SubtitleTrackSelected += OnSubtitleTrackSelected;

        // Subscribe to seek requested event
        _viewModel.SeekRequested += OnSeekRequested;
    }

    private void InitializeAutoHideTimer()
    {
        _hideControlsTimer = Dispatcher.CreateTimer();
        _hideControlsTimer.Interval = TimeSpan.FromMilliseconds(AutoHideDelayMs);
        _hideControlsTimer.Tick += OnHideControlsTimerTick;
    }

    private void SubscribeToVideoEvents()
    {
        MpvElement.PropertyChanged += OnVideoPropertyChanged;
    }

    private void OnSliderDragStarted(object? sender, EventArgs e)
    {
        _isSliderDragging = true;
    }

    private void OnSliderDragCompleted(object? sender, EventArgs e)
    {
        _isSliderDragging = false;
    }

    private void OnVideoPropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e
    )
    {
        if (string.Equals(e.PropertyName, nameof(Video.Status), StringComparison.Ordinal))
        {
            _viewModel.HandleMediaStateChanged(MpvElement.Status);

            // Reset auto-hide timer when playback state changes
            if (MpvElement.Status == VideoStatus.Playing)
            {
                ResetAutoHideTimer();
            }
        }
        else if (string.Equals(e.PropertyName, nameof(Video.Position), StringComparison.Ordinal))
        {
            _viewModel.HandlePositionChanged(MpvElement.Position);
            // Sync position to slider only when not dragging
            if (!_isSliderDragging)
            {
                PositionSlider.Position = MpvElement.Position;
            }
        }
        else if (string.Equals(e.PropertyName, nameof(Video.Duration), StringComparison.Ordinal))
        {
            _viewModel.UpdateDuration(MpvElement.Duration);
            // Sync duration to slider (XAML binding doesn't work reliably)
            PositionSlider.Duration = MpvElement.Duration;
            _logger.LogDebug(
                "[VideoPlayerPage] Duration synced to slider: {Duration}",
                MpvElement.Duration
            );

            // Load tracks when duration is available (file is loaded)
            LoadTracksFromPlayer();
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _logger.LogInformation(
            "[VideoPlayerPage] OnAppearing - VideoUrl in ViewModel: '{Url}'",
            _viewModel.VideoUrl
        );
        _logger.LogInformation(
            "[VideoPlayerPage] OnAppearing - MpvElement.Source is: {Source}",
            MpvElement.Source?.ToString() ?? "NULL"
        );

        // Set preferred orientation for video playback (landscape preferred)
        SetPreferredOrientation();

        // Show controls initially and start auto-hide timer
        ShowControls();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopAutoHideTimer();

        // Restore default orientation when leaving video player
        RestoreDefaultOrientation();

        // Pause video when page disappears (e.g., user presses home button)
        if (_viewModel.IsPlaying)
        {
            MpvElement.Pause();
            _viewModel.IsPlaying = false;
        }
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (
            query.TryGetValue(nameof(VideoPlayerViewModel.ItemId), out object? itemIdValue)
            && itemIdValue is string itemId
        )
        {
            _viewModel.ItemId = itemId;
        }

        if (
            query.TryGetValue(nameof(VideoPlayerViewModel.ItemName), out object? itemNameValue)
            && itemNameValue is string itemName
        )
        {
            _viewModel.ItemName = itemName;
        }
    }

    private void OnDisplayInfoChanged(object? sender, DisplayInfoChangedEventArgs e)
    {
        var newOrientation = e.DisplayInfo.Orientation;

        if (newOrientation != _previousOrientation)
        {
            _logger.LogInformation(
                "[VideoPlayerPage] Orientation changed from {Previous} to {New}",
                _previousOrientation,
                newOrientation
            );

            _previousOrientation = newOrientation;

            // Notify the video element about the size change (it will handle the resize)
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Force layout update for the video element
                MpvElement.InvalidateMeasure();

                // Reset auto-hide timer to show controls briefly after rotation
                if (_viewModel.IsPlaying)
                {
                    ShowControls();
                }
            });
        }
    }

    private static void SetPreferredOrientation()
    {
#if ANDROID
        var activity = Platform.CurrentActivity;
        if (activity != null)
        {
            // Allow sensor-based landscape orientation (both landscape directions)
            activity.RequestedOrientation = Android.Content.PM.ScreenOrientation.SensorLandscape;
        }
#elif IOS || MACCATALYST
        // iOS handles orientation through the Info.plist or can be controlled via UIViewController
        // For now, we allow the system to handle it
#endif
    }

    private static void RestoreDefaultOrientation()
    {
#if ANDROID
        var activity = Platform.CurrentActivity;
        if (activity != null)
        {
            // Restore to unspecified (follow system/sensor)
            activity.RequestedOrientation = Android.Content.PM.ScreenOrientation.Unspecified;
        }
#elif IOS || MACCATALYST
        // iOS handles orientation through the Info.plist
#endif
    }

    private void OnVideoTapped(object? sender, TappedEventArgs e)
    {
        ToggleControlsVisibility();
    }

    private void OnPlayPauseTapped(object? sender, TappedEventArgs e)
    {
        if (_viewModel.IsPlaying)
        {
            MpvElement.Pause();
            _viewModel.IsPlaying = false;
        }
        else
        {
            MpvElement.Play();
        }

        ResetAutoHideTimer();
    }

    private void OnSkipBackTapped(object? sender, TappedEventArgs e)
    {
        TimeSpan newPosition = MpvElement.Position - TimeSpan.FromSeconds(10);
        if (newPosition < TimeSpan.Zero)
        {
            newPosition = TimeSpan.Zero;
        }

        MpvElement.Position = newPosition;
        ResetAutoHideTimer();
    }

    private void OnSkipForwardTapped(object? sender, TappedEventArgs e)
    {
        TimeSpan newPosition = MpvElement.Position + TimeSpan.FromSeconds(10);
        if (newPosition > MpvElement.Duration)
        {
            newPosition = MpvElement.Duration;
        }

        MpvElement.Position = newPosition;
        ResetAutoHideTimer();
    }

    private void ToggleControlsVisibility()
    {
        if (_controlsVisible)
        {
            HideControls();
        }
        else
        {
            ShowControls();
        }
    }

    private async void ShowControls()
    {
        _controlsVisible = true;
        ControlsOverlay.InputTransparent = false;

        // Fade in all control elements
        await Task.WhenAll(
            TopBar.FadeToAsync(1, FadeAnimationDuration),
            BottomBar.FadeToAsync(1, FadeAnimationDuration),
            CenterPlayButton.FadeToAsync(1, FadeAnimationDuration)
        );

        ResetAutoHideTimer();
    }

    private async void HideControls()
    {
        _controlsVisible = false;
        StopAutoHideTimer();

        // Fade out all control elements
        await Task.WhenAll(
            TopBar.FadeToAsync(0, FadeAnimationDuration),
            BottomBar.FadeToAsync(0, FadeAnimationDuration),
            CenterPlayButton.FadeToAsync(0, FadeAnimationDuration)
        );

        ControlsOverlay.InputTransparent = true;
    }

    private void ResetAutoHideTimer()
    {
        if (_hideControlsTimer is null)
        {
            return;
        }

        _hideControlsTimer.Stop();

        // Only auto-hide if video is playing
        if (_viewModel.IsPlaying && _controlsVisible)
        {
            _hideControlsTimer.Start();
        }
    }

    private void StopAutoHideTimer()
    {
        _hideControlsTimer?.Stop();
    }

    private void OnHideControlsTimerTick(object? sender, EventArgs e)
    {
        _hideControlsTimer?.Stop();

        // Only hide if still playing
        if (_viewModel.IsPlaying && _controlsVisible)
        {
            HideControls();
        }
    }

    private void LoadTracksFromPlayer()
    {
        try
        {
            // Access the platform view via the handler - it's platform-specific type
            if (
                MpvElement.Handler?.PlatformView
                is not
#if ANDROID
                Mpv.Maui.Platforms.Android.MauiVideoPlayer
#elif IOS || MACCATALYST
                Mpv.Maui.Platforms.MaciOS.MauiVideoPlayer
#elif WINDOWS
                Mpv.Maui.Platforms.Windows.MauiVideoPlayer
#else
                object
#endif
                platformView
            )
            {
                _logger.LogWarning("Could not access platform view to load tracks");
                return;
            }

            IReadOnlyList<Mpv.Sys.TrackInfo> audioTracks = platformView.GetAudioTracks();
            IReadOnlyList<Mpv.Sys.TrackInfo> subtitleTracks = platformView.GetSubtitleTracks();

            _viewModel.LoadTracks(audioTracks, subtitleTracks);

            // Update current track IDs
            Mpv.Sys.TrackInfo? currentAudio = platformView.GetCurrentAudioTrack();
            Mpv.Sys.TrackInfo? currentSubtitle = platformView.GetCurrentSubtitleTrack();

            _viewModel.UpdateCurrentTracks(currentAudio?.Id, currentSubtitle?.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load tracks from player");
        }
    }

    private void OnAudioTrackSelected(object? sender, TrackSelectedEventArgs e)
    {
        try
        {
            MpvElement.SetAudioTrack(e.TrackId);
            _viewModel.UpdateCurrentTracks(e.TrackId, _viewModel.CurrentSubtitleTrackId);
            _logger.LogInformation("Audio track changed to {TrackId}", e.TrackId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change audio track to {TrackId}", e.TrackId);
        }
    }

    private void OnSubtitleTrackSelected(object? sender, TrackSelectedEventArgs e)
    {
        try
        {
            MpvElement.SetSubtitleTrack(e.TrackId);
            _viewModel.UpdateCurrentTracks(_viewModel.CurrentAudioTrackId, e.TrackId);
            _logger.LogInformation("Subtitle track changed to {TrackId}", e.TrackId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change subtitle track to {TrackId}", e.TrackId);
        }
    }

    private void OnSeekRequested(object? sender, SeekRequestedEventArgs e)
    {
        _logger.LogInformation(
            "[VideoPlayerPage] Seek requested to position: {Position}",
            e.Position
        );
        // Seek after video is loaded
        MpvElement.Seek(e.Position);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Unsubscribe from orientation changes
        DeviceDisplay.MainDisplayInfoChanged -= OnDisplayInfoChanged;

        // Unsubscribe from track selection events
        _viewModel.AudioTrackSelected -= OnAudioTrackSelected;
        _viewModel.SubtitleTrackSelected -= OnSubtitleTrackSelected;

        // Unsubscribe from seek requested event
        _viewModel.SeekRequested -= OnSeekRequested;

        if (_hideControlsTimer is not null)
        {
            _hideControlsTimer.Stop();
            _hideControlsTimer.Tick -= OnHideControlsTimerTick;
            _hideControlsTimer = null;
        }

        MpvElement.PropertyChanged -= OnVideoPropertyChanged;
        PositionSlider.DragStarted -= OnSliderDragStarted;
        PositionSlider.DragCompleted -= OnSliderDragCompleted;
    }
}
