# User Themes plugin — implementation plan

**Status (2026-10-24):** phases 0–3 implemented and building; phase 4 (testing on
the server) not done.

Records the design agreed during collaborative planning (2026-10-24). Nothing here
is new architecture; if implementation exposes an unresolved decision, stop and
raise it rather than deciding in code.

## Requirements

- Admin keeps a **library of named CSS themes** (e.g. "Halloween", "Christmas",
  "Bluey") in the plugin: add, edit, delete from the plugin config page.
- **Global swap:** one click makes any theme the server-wide custom CSS.
- **Import:** one button saves the current Dashboard > Branding > Custom CSS as a
  new theme (e.g. the existing Halloween CSS).
- **Per-user assignment:** any user can be assigned a theme. The assigned theme
  **replaces** the global CSS for that user (it is not layered on top) and **always
  wins** over the global CSS.
- **Preview as me:** an admin can apply a theme to their own account only, until
  they stop the preview, then browse the real UI to see it.
- **Admins only.** Users cannot set or change their own theme.
- **Out of scope:** scheduling / date windows for global themes (deferred).

## Approach

1. **Storage — `PluginConfiguration`** (plugin XML, saved through the standard plugin
   configuration API from the config page, like the other plugins):
   - `Themes`: list of `{ Id (Guid), Name, Css }`
   - `UserThemes`: list of `{ UserId, ThemeId }` — at most one per user
   - `PreviewThemes`: list of `{ UserId, ThemeId }` — at most one per user
2. **One MVC action filter** (`IAsyncActionFilter`, registered via
   `MvcOptions.Filters.AddService` exactly as smart-library does) on the
   `Branding` controller actions `GetBrandingOptions` (`GET /Branding/Configuration`)
   and `GetBrandingCss` (`GET /Branding/Css`, `/Branding/Css.css`):
   - Resolve the requesting user with the same rule as smart-library's
     `FilterHelpers.GetUserId` (authenticated `Jellyfin-UserId` claim). Anonymous
     requests are left untouched.
   - Effective theme = **preview for that user**, else **assigned theme**, else none.
   - If there is one, replace the CSS in the result: `BrandingOptionsDto.CustomCss`
     for `/Branding/Configuration`, the string body for `/Branding/Css`. Otherwise
     leave the response alone (global Branding CSS applies).
   - A theme reference whose theme no longer exists is treated as none.
3. **Global swap writes into Branding** (no filter involvement):
   - The config page reads the global branding with `GET /System/Configuration/branding`
     — **not** `/Branding/Configuration`, which the filter rewrites for the admin's
     own preview/assignment.
   - Activate = re-read `GET /System/Configuration/branding`, set `CustomCss` to the
     theme's CSS and `POST /System/Configuration/branding` with the full object, so
     every other branding setting (including `SplashscreenLocation`) is kept. Used
     instead of `POST /System/Configuration/Branding` (DTO endpoint), because on a
     server without that endpoint the same URL hits the generic named-config action
     and would drop `SplashscreenLocation`.
   - "Active" is derived, not stored: the theme whose CSS exactly equals the Branding
     CSS. No match and non-empty ⇒ shown as **"Custom (not a saved theme)"**.
   - Import = create a theme from the Branding CSS (prompt for a name).
4. **No custom API controller or service.** The config page only uses built-in
   endpoints: plugin configuration, branding configuration, `/Users`, and
   `ApiClient.getCurrentUserId()` for preview.

Effective CSS for a user:
**preview (self) → assigned theme → Branding > Custom CSS → none.**

## Behaviour details (agreed assumptions)

- Plugin name **"User Themes"**, new GUID.
- Saving edits to the **currently active** global theme also updates Branding.
- Deleting a theme: confirm first; remove any user assignments / previews using it.
  If it is the active global theme, Branding is left as it is (now shows "Custom").
- A theme with **empty CSS** is valid: assigning it gives that user no custom CSS.
- Preview persists (stored in config, survives restarts) until stopped, and takes
  priority over the admin's own assignment.
- Accepted limitation: jellyfin-web's local *Settings > Display > Disable server
  custom CSS* lets a user hide their theme on that device. Not blocked.

## Known platform limits (not fixable server-side)

- jellyfin-web caches branding (`['BrandingOptions']`, ~60 s stale time, persisted
  to IndexedDB, cleared on logout). Changes reach an open client after a reload or
  about a minute. Switching user logs out, so the new user gets their own CSS.
- Custom CSS is never applied on dashboard pages, or before login.
- Applies to clients running jellyfin-web (browser, Android/iOS apps, Jellyfin
  Desktop, Xbox, webOS, Tizen). Native clients (Android TV, Swiftfin, Roku, Kodi)
  never render custom CSS.

## Implementation phases

### Phase 0 — scaffolding
Copy smart-library's layout and conventions: `Jellyfin.Plugin.UserThemes.csproj`
(net10.0, `Jellyfin.Controller`/`Jellyfin.Model` 12.1.0 with `ExcludeAssets=runtime`,
`FrameworkReference Microsoft.AspNetCore.App`, StyleCop + SerilogAnalyzer,
`TreatWarningsAsErrors`), `global.json`, `Directory.Build.props`, `stylecop.json`,
`.gitignore`, `build.yaml`, `manifest.json`, `.github/workflows/release.yaml`,
`LICENSE`. `Plugin.cs` (`BasePlugin<PluginConfiguration>, IHasWebPages`) and
`PluginServiceRegistrator.cs`.

### Phase 1 — configuration model
`Configuration/PluginConfiguration.cs` plus `Models/Theme.cs` and
`Models/UserThemeAssignment.cs` (shared by `UserThemes` and `PreviewThemes`).

### Phase 2 — branding filter
`Filters/BrandingFilter.cs` as described in Approach §2, with a small helper for user
resolution (copied from smart-library `FilterHelpers`, trimmed to what is used).
Register in `PluginServiceRegistrator`.

### Phase 3 — config page
`Configuration/configPage.html` (embedded resource):
- **Themes:** list with name, "Active" / preview indicators; add / edit (name + CSS
  textarea) / delete; **Activate globally**; **Preview as me** / **Stop preview**.
- **Global:** shows the active theme or "Custom (not a saved theme)" / "None";
  **Save current Branding CSS as theme**.
- **Users:** each user with a theme dropdown ("Use global" + themes).

### Phase 4 — verify on the server
- Bluey assigned to the son's account: he sees Bluey; other users see Halloween;
  clearing Branding CSS leaves others with none and the son still with Bluey.
- Activate swaps Branding > Custom CSS; Dashboard Branding page shows it; import works.
- Preview applies only to the admin and stops cleanly.
- `/Branding/Css` anonymous returns the global CSS; with auth returns the user's.
- Branding's other settings (login disclaimer, splash screen) are unchanged after
  Activate.
