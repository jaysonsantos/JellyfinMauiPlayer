using System.Windows.Input;

namespace Mpv.Maui.Controls;

public class PositionSlider : Slider
{
    public static readonly BindableProperty DurationProperty = BindableProperty.Create(
        nameof(Duration),
        typeof(TimeSpan),
        typeof(PositionSlider),
        TimeSpan.FromSeconds(1),
        propertyChanged: (bindable, _, newValue) =>
        {
            double seconds = ((TimeSpan)newValue).TotalSeconds;
            ((Slider)bindable).Maximum = seconds <= 0 ? 1 : seconds;
        }
    );

    public static readonly BindableProperty PositionProperty = BindableProperty.Create(
        nameof(Position),
        typeof(TimeSpan),
        typeof(PositionSlider),
        new TimeSpan(0),
        defaultBindingMode: BindingMode.TwoWay,
        propertyChanged: (bindable, oldValue, newValue) =>
        {
            var slider = (PositionSlider)bindable;
            if (!slider._isUpdatingFromSlider)
            {
                double seconds = ((TimeSpan)newValue).TotalSeconds;
                slider.Value = seconds;
            }
        }
    );

    public static readonly BindableProperty SeekCommandProperty = BindableProperty.Create(
        nameof(SeekCommand),
        typeof(ICommand),
        typeof(PositionSlider),
        null
    );

    public TimeSpan Duration
    {
        get { return (TimeSpan)GetValue(DurationProperty); }
        set { SetValue(DurationProperty, value); }
    }

    public TimeSpan Position
    {
        get { return (TimeSpan)GetValue(PositionProperty); }
        set { SetValue(PositionProperty, value); }
    }

    public ICommand? SeekCommand
    {
        get { return (ICommand?)GetValue(SeekCommandProperty); }
        set { SetValue(SeekCommandProperty, value); }
    }

    private bool _isUpdatingFromSlider;
    private bool _isDragging;

    public PositionSlider()
    {
        DragStarted += OnDragStarted;
        DragCompleted += OnDragCompleted;

        ValueChanged += (sender, args) =>
        {
            if (_isUpdatingFromSlider)
                return;

            // Don't seek while dragging - wait for DragCompleted
            if (_isDragging)
                return;

            TimeSpan newPosition = TimeSpan.FromSeconds(args.NewValue);

            // Check if change is significant enough to warrant an update
            if (
                Math.Abs(newPosition.TotalSeconds - Position.TotalSeconds) / Duration.TotalSeconds
                <= 0.01
            )
                return;

            // Not dragging, seek immediately (e.g., when user taps on slider track)
            ExecuteSeek(args.NewValue);
        };
    }

    private void OnDragStarted(object? sender, EventArgs e)
    {
        _isDragging = true;
    }

    private void OnDragCompleted(object? sender, EventArgs e)
    {
        _isDragging = false;

        // Seek to final value when drag completes
        ExecuteSeek(Value);
    }

    private void ExecuteSeek(double seconds)
    {
        TimeSpan newPosition = TimeSpan.FromSeconds(seconds);

        _isUpdatingFromSlider = true;
        try
        {
            if (SeekCommand?.CanExecute(newPosition) == true)
            {
                SeekCommand.Execute(newPosition);
            }
        }
        finally
        {
            _isUpdatingFromSlider = false;
        }
    }
}
