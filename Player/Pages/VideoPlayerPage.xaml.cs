using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
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

    private void OnMediaStateChanged(object? sender, MediaStateChangedEventArgs e)
    {
        _viewModel.HandleMediaStateChanged(e.NewState);

        if (
            (e.NewState == MediaElementState.Opening || e.NewState == MediaElementState.Playing)
            && sender is MediaElement element
            && element.Duration.TotalSeconds > 0
        )
        {
            _viewModel.UpdateDuration(element.Duration);
        }
    }

    private void OnPositionChanged(object? sender, MediaPositionChangedEventArgs e)
    {
        _viewModel.HandlePositionChanged(e.Position);

        if (sender is MediaElement element && element.Duration.TotalSeconds > 0)
        {
            _viewModel.UpdateDuration(element.Duration);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        MediaElement.Stop();
    }
}
