using Player.ViewModels;

namespace Player.Pages;

public partial class LibraryPage : ContentPage
{
    private readonly LibraryViewModel _viewModel;

    public LibraryPage(LibraryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateGridSpan();
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        UpdateGridSpan();
    }

    private void UpdateGridSpan()
    {
        const double minCardWidth = 180.0;
        const double spacing = 16.0;
        const double padding = 40.0;

        double availableWidth = Width - padding;
        if (availableWidth <= 0)
            return;

        int calculatedSpan = (int)Math.Floor((availableWidth + spacing) / (minCardWidth + spacing));

        _viewModel.GridSpan = Math.Max(2, Math.Min(calculatedSpan, 8));
    }
}
