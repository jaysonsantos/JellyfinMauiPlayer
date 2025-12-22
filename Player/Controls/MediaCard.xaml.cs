using System.Windows.Input;
using JellyfinPlayer.Lib.Models;

namespace Player.Controls;

public partial class MediaCard : ContentView
{
    public static readonly BindableProperty MediaItemProperty = BindableProperty.Create(
        nameof(MediaItem),
        typeof(MediaItem),
        typeof(MediaCard),
        null
    );

    public static readonly BindableProperty TappedCommandProperty = BindableProperty.Create(
        nameof(TappedCommand),
        typeof(ICommand),
        typeof(MediaCard),
        null
    );

    public static readonly BindableProperty CardWidthProperty = BindableProperty.Create(
        nameof(CardWidth),
        typeof(double),
        typeof(MediaCard),
        150.0
    );

    public static readonly BindableProperty CardHeightProperty = BindableProperty.Create(
        nameof(CardHeight),
        typeof(double),
        typeof(MediaCard),
        250.0
    );

    public static readonly BindableProperty ImageHeightProperty = BindableProperty.Create(
        nameof(ImageHeight),
        typeof(double),
        typeof(MediaCard),
        200.0
    );

    public static readonly BindableProperty ShowTitleProperty = BindableProperty.Create(
        nameof(ShowTitle),
        typeof(bool),
        typeof(MediaCard),
        true
    );

    public static readonly BindableProperty ShowTypeProperty = BindableProperty.Create(
        nameof(ShowType),
        typeof(bool),
        typeof(MediaCard),
        false
    );

    public static readonly BindableProperty ShowYearProperty = BindableProperty.Create(
        nameof(ShowYear),
        typeof(bool),
        typeof(MediaCard),
        false
    );

    public MediaItem? MediaItem
    {
        get => (MediaItem?)GetValue(MediaItemProperty);
        set => SetValue(MediaItemProperty, value);
    }

    public ICommand? TappedCommand
    {
        get => (ICommand?)GetValue(TappedCommandProperty);
        set => SetValue(TappedCommandProperty, value);
    }

    public double CardWidth
    {
        get => (double)GetValue(CardWidthProperty);
        set => SetValue(CardWidthProperty, value);
    }

    public double CardHeight
    {
        get => (double)GetValue(CardHeightProperty);
        set => SetValue(CardHeightProperty, value);
    }

    public double ImageHeight
    {
        get => (double)GetValue(ImageHeightProperty);
        set => SetValue(ImageHeightProperty, value);
    }

    public bool ShowTitle
    {
        get => (bool)GetValue(ShowTitleProperty);
        set => SetValue(ShowTitleProperty, value);
    }

    public bool ShowType
    {
        get => (bool)GetValue(ShowTypeProperty);
        set => SetValue(ShowTypeProperty, value);
    }

    public bool ShowYear
    {
        get => (bool)GetValue(ShowYearProperty);
        set => SetValue(ShowYearProperty, value);
    }

    public MediaCard()
    {
        InitializeComponent();
    }
}
