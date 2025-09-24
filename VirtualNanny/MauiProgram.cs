using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using MudBlazor.Services;

namespace VirtualNanny
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            // Add Blazor services
            builder.Services.AddMauiBlazorWebView();
            
            // Add MudBlazor services
            builder.Services.AddMudServices();

            // For .NET 9+ - MAUI Blazor WebView handles static assets automatically
            // The fingerprinting is handled by the WebView component itself

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
            builder.Logging.SetMinimumLevel(LogLevel.Debug);
#else
            builder.Logging.SetMinimumLevel(LogLevel.Warning);
#endif

            return builder.Build();
        }
    }
}
