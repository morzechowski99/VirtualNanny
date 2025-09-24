using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Diagnostics;

namespace VirtualNanny.Components.Layout;

public partial class MainLayout : LayoutComponentBase
{
    private bool _drawerOpen;
    private bool _isDarkMode;
    private MudThemeProvider _mudThemeProvider = null!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                _isDarkMode = await _mudThemeProvider.GetSystemDarkModeAsync();
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting system preference: {ex.Message}");
            }
        }
    }

    private void ToggleDrawer()
    {
        _drawerOpen = !_drawerOpen;
    }

    private void ToggleDarkMode()
    {
        _isDarkMode = !_isDarkMode;
    }
}