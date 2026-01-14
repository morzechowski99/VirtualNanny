using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using VirtualNanny.AudioAnalysis;
using VirtualNanny.AudioAnalysis.Interfaces;
using VirtualNanny.Components;
using VirtualNanny.Services;
using VirtualNanny.Services.Interfaces;

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

        // Register audio analysis services
        builder.Services.AddSingleton<ICryDetector, ThresholdCryDetector>();

        // Register detection service
        builder.Services.AddSingleton<ICryDetectionService, CryDetectionService>();

        // Register notification service (platform-independent)
        builder.Services.AddSingleton<INotificationService, NotificationService>();

        // Register platform-specific services
#if ANDROID
        builder.Services.AddSingleton<IFileService, AndroidFileService>();
        builder.Services.AddSingleton<IRTSPStreamService, AndroidRTSPStreamService>();
        builder.Services.AddSingleton<IRecordingService, AndroidRecordingService>();
        builder.Services.AddSingleton<IMediaService, AndroidMediaService>();
        
        // Configure custom WebView handler for Android
        builder.ConfigureMauiHandlers(handlers =>
        {
#if ANDROID
            handlers.AddHandler<BlazorWebView, CameraBlazorWebViewHandler>();
#endif
        });
#elif WINDOWS
        builder.Services.AddSingleton<IFileService, WindowsFileService>();
        builder.Services.AddSingleton<IRTSPStreamService, WindowsRTSPStreamService>();
        builder.Services.AddSingleton<IRecordingService, WindowsRecordingService>();
        builder.Services.AddSingleton<IMediaService, WindowsMediaService>();
#elif IOS
        builder.Services.AddSingleton<IFileService, iOSFileService>();
        // TODO: Add iOS-specific implementations of IRTSPStreamService, IRecordingService, IMediaService
#elif MACCATALYST
        builder.Services.AddSingleton<IFileService, MacCatalystFileService>();
        // TODO: Add macCatalyst-specific implementations of IRTSPStreamService, IRecordingService, IMediaService
#endif

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
