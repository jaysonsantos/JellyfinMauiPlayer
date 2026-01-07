namespace Mpv.Maui.Controls;

/// <summary>
/// Event arguments for subtitle file path.
/// </summary>
/// <param name="filePath">The path to the subtitle file.</param>
public sealed class SubtitleFileEventArgs(string filePath) : EventArgs
{
    /// <summary>
    /// Gets the path to the subtitle file.
    /// </summary>
    public string FilePath { get; } = filePath;
}
