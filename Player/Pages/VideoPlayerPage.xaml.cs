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

    private const uint FadeAnimationDuration = 250;
    private const int AutoHideDelayMs = 4000;

    public VideoPlayerPage(VideoPlayerViewModel viewModel, ILogger<VideoPlayerPage> logger)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _logger = logger;
        BindingContext = viewModel;

        _logger.LogInformation(
            "[VideoPlayerPage] Constructor - VideoUrl in ViewModel: '{Url}'",
            _viewModel.VideoUrl
        );

        InitializeAutoHideTimer();
        SubscribeToVideoEvents();
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

    private void OnVideoPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
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
        }
        else if (string.Equals(e.PropertyName, nameof(Video.Duration), StringComparison.Ordinal))
        {
            _viewModel.UpdateDuration(MpvElement.Duration);
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

        // Show controls initially and start auto-hide timer
        ShowControls();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopAutoHideTimer();
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

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_hideControlsTimer is not null)
        {
            _hideControlsTimer.Stop();
            _hideControlsTimer.Tick -= OnHideControlsTimerTick;
            _hideControlsTimer = null;
        }

        MpvElement.PropertyChanged -= OnVideoPropertyChanged;
    }
}
