using System;

namespace Jellyfin.Plugin.UserThemes.Models;

/// <summary>
/// A saved custom CSS theme.
/// </summary>
public class Theme
{
    /// <summary>
    /// Gets or sets the theme id.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the theme name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the theme's CSS. Empty means no custom CSS.
    /// </summary>
    public string Css { get; set; } = string.Empty;
}
