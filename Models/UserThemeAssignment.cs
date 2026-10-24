using System;

namespace Jellyfin.Plugin.UserThemes.Models;

/// <summary>
/// A theme applied to a single user.
/// </summary>
public class UserThemeAssignment
{
    /// <summary>
    /// Gets or sets the user id.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the theme id.
    /// </summary>
    public Guid ThemeId { get; set; }
}
