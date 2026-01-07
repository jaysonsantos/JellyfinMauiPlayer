using System.ComponentModel;
using System.Windows.Input;

namespace Mpv.Maui.Controls
{
    public class Video : View, IVideoController, IDisposable
    {
        private bool _disposed;

        #region Bindable Properties

        public static readonly BindableProperty AreTransportControlsEnabledProperty =
            BindableProperty.Create(
                nameof(AreTransportControlsEnabled),
                typeof(bool),
                typeof(Video),
                true
            );

        public static readonly BindableProperty SourceProperty = BindableProperty.Create(
            nameof(Source),
            typeof(VideoSource),
            typeof(Video)
        );

        public static readonly BindableProperty AutoPlayProperty = BindableProperty.Create(
            nameof(AutoPlay),
            typeof(bool),
            typeof(Video),
            true
        );

        public static readonly BindableProperty IsLoopingProperty = BindableProperty.Create(
            nameof(IsLooping),
            typeof(bool),
            typeof(Video),
            false
        );

        private static readonly BindablePropertyKey StatusPropertyKey =
            BindableProperty.CreateReadOnly(
                nameof(Status),
                typeof(VideoStatus),
                typeof(Video),
                VideoStatus.NotReady
            );

        public static readonly BindableProperty StatusProperty = StatusPropertyKey.BindableProperty;

        private static readonly BindablePropertyKey DurationPropertyKey =
            BindableProperty.CreateReadOnly(
                nameof(Duration),
                typeof(TimeSpan),
                typeof(Video),
                TimeSpan.Zero,
                propertyChanged: (bindable, _, _) => ((Video)bindable).SetTimeToEnd()
            );

        public static readonly BindableProperty DurationProperty =
            DurationPropertyKey.BindableProperty;

        public static readonly BindableProperty PositionProperty = BindableProperty.Create(
            nameof(Position),
            typeof(TimeSpan),
            typeof(Video),
            TimeSpan.Zero,
            propertyChanged: (bindable, _, _) => ((Video)bindable).SetTimeToEnd()
        );

        private static readonly BindablePropertyKey TimeToEndPropertyKey =
            BindableProperty.CreateReadOnly(
                nameof(TimeToEnd),
                typeof(TimeSpan),
                typeof(Video),
                TimeSpan.Zero
            );

        public static readonly BindableProperty TimeToEndProperty =
            TimeToEndPropertyKey.BindableProperty;

        private static readonly BindablePropertyKey SeekCommandPropertyKey =
            BindableProperty.CreateReadOnly(
                nameof(SeekCommand),
                typeof(ICommand),
                typeof(Video),
                null,
                defaultValueCreator: (bindable) =>
                {
                    var video = (Video)bindable;
                    return new Command<TimeSpan>(position => video.Seek(position));
                }
            );

        public static readonly BindableProperty SeekCommandProperty =
            SeekCommandPropertyKey.BindableProperty;

        public bool AreTransportControlsEnabled
        {
            get { return (bool)GetValue(AreTransportControlsEnabledProperty); }
            set { SetValue(AreTransportControlsEnabledProperty, value); }
        }

        [TypeConverter(typeof(VideoSourceConverter))]
        public VideoSource Source
        {
            get { return (VideoSource)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public bool AutoPlay
        {
            get { return (bool)GetValue(AutoPlayProperty); }
            set { SetValue(AutoPlayProperty, value); }
        }

        public bool IsLooping
        {
            get { return (bool)GetValue(IsLoopingProperty); }
            set { SetValue(IsLoopingProperty, value); }
        }

        public VideoStatus Status
        {
            get { return (VideoStatus)GetValue(StatusProperty); }
        }

        VideoStatus IVideoController.Status
        {
            get { return Status; }
            set { SetValue(StatusPropertyKey, value); }
        }

        public TimeSpan Duration
        {
            get { return (TimeSpan)GetValue(DurationProperty); }
        }

        TimeSpan IVideoController.Duration
        {
            get { return Duration; }
            set { SetValue(DurationPropertyKey, value); }
        }

        public TimeSpan Position
        {
            get { return (TimeSpan)GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }

        public TimeSpan TimeToEnd
        {
            get { return (TimeSpan)GetValue(TimeToEndProperty); }
            private set { SetValue(TimeToEndPropertyKey, value); }
        }

        public ICommand SeekCommand
        {
            get { return (ICommand)GetValue(SeekCommandProperty); }
        }

        #endregion

        #region Events

        public event EventHandler? UpdateStatus;
        public event EventHandler? UpdateSize;
        public event EventHandler<VideoPositionEventArgs>? PlayRequested;
        public event EventHandler<VideoPositionEventArgs>? PauseRequested;
        public event EventHandler<VideoPositionEventArgs>? StopRequested;
        public event EventHandler<VideoPositionEventArgs>? SeekRequested;
        public event EventHandler<int>? AudioTrackChangeRequested;
        public event EventHandler<int>? SubtitleTrackChangeRequested;
        public event EventHandler? GetTracksRequested;

        #endregion


        public Video()
        {
            // Subscribe to size changes (VisualElement.SizeChanged)
            SizeChanged += OnViewSizeChanged;
        }

        void OnViewSizeChanged(object? sender, EventArgs e)
        {
            // Notify handler about size change
            UpdateSize?.Invoke(this, EventArgs.Empty);
            Handler?.Invoke(nameof(UpdateSize));
        }

        public void Play()
        {
            VideoPositionEventArgs args = new();
            PlayRequested?.Invoke(this, args);
            Handler?.Invoke(nameof(PlayRequested), args);
        }

        public void Pause()
        {
            VideoPositionEventArgs args = new(Position);
            PauseRequested?.Invoke(this, args);
            Handler?.Invoke(nameof(PauseRequested), args);
        }

        public void Stop()
        {
            VideoPositionEventArgs args = new(Position);
            StopRequested?.Invoke(this, args);
            Handler?.Invoke(nameof(StopRequested), args);
        }

        public void Seek(TimeSpan position)
        {
            VideoPositionEventArgs args = new(position);
            SeekRequested?.Invoke(this, args);
            Handler?.Invoke(nameof(SeekRequested), args);
        }

        public void SetAudioTrack(int trackId)
        {
            AudioTrackChangeRequested?.Invoke(this, trackId);
            Handler?.Invoke(nameof(AudioTrackChangeRequested), trackId);
        }

        public void SetSubtitleTrack(int trackId)
        {
            SubtitleTrackChangeRequested?.Invoke(this, trackId);
            Handler?.Invoke(nameof(SubtitleTrackChangeRequested), trackId);
        }

        public void RequestGetTracks()
        {
            GetTracksRequested?.Invoke(this, EventArgs.Empty);
            Handler?.Invoke(nameof(GetTracksRequested), EventArgs.Empty);
        }

        void SetTimeToEnd()
        {
            TimeToEnd = Duration - Position;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (disposing)
            {
                // Unsubscribe from size changes
                SizeChanged -= OnViewSizeChanged;
                // Clear event handlers to prevent memory leaks
                UpdateStatus = null;
                UpdateSize = null;
                PlayRequested = null;
                PauseRequested = null;
                StopRequested = null;
                SeekRequested = null;
                AudioTrackChangeRequested = null;
                SubtitleTrackChangeRequested = null;
                GetTracksRequested = null;
            }
        }
    }
}
