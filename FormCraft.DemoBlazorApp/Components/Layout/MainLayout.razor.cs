using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Layout;

public partial class MainLayout
{
    private bool _drawerOpen = true;
    private bool _isDarkMode;
    private string _version = "loading...";

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _version = await VersionService.GetFormCraftVersionAsync();
        }
        catch
        {
            _version = "latest";
        }
    }

    private readonly MudTheme _theme = new()
    {
        PaletteLight = new PaletteLight()
        {
            Primary = Colors.Blue.Default,
            Secondary = Colors.Teal.Default,
            AppbarBackground = Colors.Blue.Default,
            AppbarText = Colors.Shades.White,
            DrawerBackground = "#f5f5f5",
            DrawerText = "#424242",
            DrawerIcon = "#616161"
        },
        PaletteDark = new PaletteDark()
        {
            Primary = Colors.Blue.Lighten1,
            Secondary = Colors.Teal.Lighten1,
            AppbarBackground = "#212121",
            AppbarText = Colors.Shades.White,
            DrawerBackground = "#212121",
            DrawerText = "#e0e0e0",
            DrawerIcon = "#bdbdbd",
            Surface = "#424242",
            Background = "#303030",
            BackgroundGray = "#424242"
        }
    };

    private void ToggleDrawer()
    {
        _drawerOpen = !_drawerOpen;
    }

    private void ToggleTheme()
    {
        _isDarkMode = !_isDarkMode;
    }

    private void NavigateToHome()
    {
        Navigation.NavigateTo("home");
    }
}