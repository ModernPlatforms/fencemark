using MudBlazor;

namespace fencemark.Web.Theme;

public static class FencemarkTheme
{
    public static MudTheme DefaultTheme => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#667eea",
            Secondary = "#764ba2",
            AppbarBackground = "#667eea",
            Background = "#f8f9fa",
            Surface = "#ffffff",
            DrawerBackground = "#ffffff",
            DrawerText = "rgba(0,0,0, 0.87)",
            DrawerIcon = "rgba(0,0,0, 0.54)",
            Success = "#28a745",
            Info = "#17a2b8",
            Warning = "#ffc107",
            Error = "#dc3545",
            Dark = "#343a40",
            TextPrimary = "rgba(0,0,0, 0.87)",
            TextSecondary = "rgba(0,0,0, 0.60)",
            ActionDefault = "rgba(0,0,0, 0.54)",
            ActionDisabled = "rgba(0,0,0, 0.26)",
            ActionDisabledBackground = "rgba(0,0,0, 0.12)"
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#667eea",
            Secondary = "#764ba2",
            AppbarBackground = "#1e1e1e",
            Background = "#121212",
            Surface = "#1e1e1e",
            DrawerBackground = "#1e1e1e",
            DrawerText = "rgba(255,255,255, 0.87)",
            DrawerIcon = "rgba(255,255,255, 0.70)",
            Success = "#28a745",
            Info = "#17a2b8",
            Warning = "#ffc107",
            Error = "#dc3545",
            Dark = "#212529",
            TextPrimary = "rgba(255,255,255, 0.87)",
            TextSecondary = "rgba(255,255,255, 0.60)",
            ActionDefault = "rgba(255,255,255, 0.70)",
            ActionDisabled = "rgba(255,255,255, 0.26)",
            ActionDisabledBackground = "rgba(255,255,255, 0.12)"
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px",
            DrawerWidthLeft = "260px",
            DrawerWidthRight = "300px"
        },
        ZIndex = new ZIndex
        {
            Drawer = 1200,
            Dialog = 1300,
            Snackbar = 1400,
            Tooltip = 1500
        }
    };
}
