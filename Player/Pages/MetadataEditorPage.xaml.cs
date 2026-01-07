using Player.ViewModels;

namespace Player.Pages;

public partial class MetadataEditorPage : ContentPage
{
    public MetadataEditorPage(MetadataEditorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
