namespace Player.Controls;

public partial class LoadingCard : ContentView
{
    public static readonly BindableProperty CardWidthProperty = BindableProperty.Create(
        nameof(CardWidth),
        typeof(double),
        typeof(LoadingCard),
        150.0
    );

    public static readonly BindableProperty CardHeightProperty = BindableProperty.Create(
        nameof(CardHeight),
        typeof(double),
        typeof(LoadingCard),
        250.0
    );

    public static readonly BindableProperty ImageHeightProperty = BindableProperty.Create(
        nameof(ImageHeight),
        typeof(double),
        typeof(LoadingCard),
        200.0
    );

    public static readonly BindableProperty ShowTitleProperty = BindableProperty.Create(
        nameof(ShowTitle),
        typeof(bool),
        typeof(LoadingCard),
        true
    );

    public static readonly BindableProperty ShowMetadataProperty = BindableProperty.Create(
        nameof(ShowMetadata),
        typeof(bool),
        typeof(LoadingCard),
        false
    );

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

    public bool ShowMetadata
    {
        get => (bool)GetValue(ShowMetadataProperty);
        set => SetValue(ShowMetadataProperty, value);
    }

    public LoadingCard()
    {
        InitializeComponent();
    }
}
