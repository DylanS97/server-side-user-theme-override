using System;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.UserThemes.Configuration;
using MediaBrowser.Model.Branding;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Jellyfin.Plugin.UserThemes.Filters;

/// <summary>
/// Replaces the server's custom CSS in <c>/Branding/Configuration</c> and <c>/Branding/Css</c> with the theme
/// the requesting user is previewing or has been assigned.
/// </summary>
public class BrandingFilter : IAsyncActionFilter
{
    private const string UserIdClaimType = "Jellyfin-UserId";

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor { ControllerName: "Branding" } descriptor
            || descriptor.ActionName is not ("GetBrandingOptions" or "GetBrandingCss"))
        {
            await next().ConfigureAwait(false);
            return;
        }

        var css = GetThemeCss(context);
        var executed = await next().ConfigureAwait(false);

        if (css is null || executed.Result is not ObjectResult objectResult)
        {
            return;
        }

        switch (objectResult.Value)
        {
            case BrandingOptionsDto options:
                options.CustomCss = css;
                break;
            case string:
                objectResult.Value = css;
                break;
        }
    }

    /// <summary>
    /// Gets the CSS of the theme the authenticated user is previewing, or else has been assigned.
    /// </summary>
    /// <param name="context">The filter context.</param>
    /// <returns>The theme CSS, or null if the global custom CSS applies.</returns>
    private static string? GetThemeCss(ActionExecutingContext context)
    {
        var config = Plugin.Instance?.Configuration;
        var claim = context.HttpContext.User.Claims
            .FirstOrDefault(c => c.Type.Equals(UserIdClaimType, StringComparison.OrdinalIgnoreCase))?.Value;
        if (config is null || !Guid.TryParse(claim, out var userId) || userId.Equals(Guid.Empty))
        {
            return null;
        }

        return FindThemeCss(config, config.PreviewThemes.FirstOrDefault(a => a.UserId.Equals(userId))?.ThemeId)
            ?? FindThemeCss(config, config.UserThemes.FirstOrDefault(a => a.UserId.Equals(userId))?.ThemeId);
    }

    private static string? FindThemeCss(PluginConfiguration config, Guid? themeId)
        => themeId.HasValue ? config.Themes.FirstOrDefault(t => t.Id.Equals(themeId.Value))?.Css : null;
}
