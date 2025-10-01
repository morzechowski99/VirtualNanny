using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using VirtualNanny.Components;
using VirtualNanny.Services;

#if ANDROID
using VirtualNanny.Platforms.Android;
using VirtualNanny.Platforms.Android.Services;
using Microsoft.AspNetCore.Components.WebView.Maui;
#elif WINDOWS
using VirtualNanny.Platforms.Windows.Services;
#elif IOS
using VirtualNanny.Platforms.iOS.Services;
#elif MACCATALYST
using VirtualNanny.Platforms.MacCatalyst.Services;
#endif

namespace VirtualNanny;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Register Blazor components and services
        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddMudServices();

        // Register file service for each platform
#if ANDROID
        builder.Services.AddSingleton<IFileService, AndroidFileService>();
        
        // Configure custom WebView handler for Android
        builder.ConfigureMauiHandlers(handlers =>
        {
#if ANDROID
            handlers.AddHandler<BlazorWebView, CameraBlazorWebViewHandler>();
#endif
        });
#elif WINDOWS
        builder.Services.AddSingleton<IFileService, WindowsFileService>();
#elif IOS
        builder.Services.AddSingleton<IFileService, iOSFileService>();
#elif MACCATALYST
        builder.Services.AddSingleton<IFileService, MacCatalystFileService>();
#endif

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
