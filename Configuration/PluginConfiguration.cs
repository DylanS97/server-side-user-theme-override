using Jellyfin.Plugin.UserThemes.Models;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.UserThemes.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets the saved themes.
    /// </summary>
    public Theme[] Themes { get; set; } = [];

    /// <summary>
    /// Gets or sets the themes assigned to users. These replace the server's custom CSS for that user.
    /// </summary>
    public UserThemeAssignment[] UserThemes { get; set; } = [];

    /// <summary>
    /// Gets or sets the themes admins are previewing on their own account. A preview wins over an assignment.
    /// </summary>
    public UserThemeAssignment[] PreviewThemes { get; set; } = [];
}
