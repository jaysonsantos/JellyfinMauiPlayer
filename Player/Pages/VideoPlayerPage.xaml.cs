using Player.ViewModels;

namespace Player.Pages;

public sealed partial class VideoPlayerPage : ContentPage, IQueryAttributable
{
    private readonly VideoPlayerViewModel _viewModel;

    public VideoPlayerPage(VideoPlayerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
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
}
