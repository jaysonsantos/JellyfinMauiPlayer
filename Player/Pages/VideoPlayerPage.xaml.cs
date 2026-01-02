using Microsoft.Extensions.Logging;
using Player.ViewModels;

namespace Player.Pages;

public sealed partial class VideoPlayerPage : ContentPage, IQueryAttributable
{
    private readonly VideoPlayerViewModel _viewModel;
    private readonly ILogger<VideoPlayerPage> _logger;

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
