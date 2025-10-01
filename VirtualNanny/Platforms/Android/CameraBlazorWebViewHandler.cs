using Android = global::Android;
using Android.Webkit;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Android.Content;
using AndroidX.Core.Content;
using Android.Provider;
using Java.IO;
using Android.OS;
using Android.App;
using AndroidX.Core.App;

namespace VirtualNanny.Platforms.Android
{
    public class CameraBlazorWebViewHandler : BlazorWebViewHandler
    {
        protected override void ConnectHandler(global::Android.Webkit.WebView platformView)
        {
            base.ConnectHandler(platformView);

            global::Android.Util.Log.Info("VirtualNanny", "Connecting custom WebView handler for camera support");

            // Configure WebView settings for camera access
            var settings = platformView.Settings;
            if (settings != null)
            {
                settings.MediaPlaybackRequiresUserGesture = false;
                settings.AllowContentAccess = true;
                settings.AllowFileAccess = true;
                settings.AllowUniversalAccessFromFileURLs = true;
                settings.AllowFileAccessFromFileURLs = true;
                settings.JavaScriptEnabled = true;
                settings.DomStorageEnabled = true;
                settings.DatabaseEnabled = true;

                // Additional settings for media access
                settings.MixedContentMode = MixedContentHandling.AlwaysAllow;

                global::Android.Util.Log.Info("VirtualNanny", "WebView settings configured for media access");
            }

            // Set custom WebChromeClient to handle permission requests
            platformView.SetWebChromeClient(new CameraWebChromeClient());

            // Set download listener for file downloads
            platformView.SetDownloadListener(new CameraDownloadListener());

            // Log that handler is connected
            global::Android.Util.Log.Info("VirtualNanny", "Custom WebView handler connected with download support");
        }
    }

    public class CameraDownloadListener : Java.Lang.Object, IDownloadListener
    {
        public void OnDownloadStart(string? url, string? userAgent, string? contentDisposition, string? mimetype, long contentLength)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    global::Android.Util.Log.Warn("VirtualNanny", "Download URL is null or empty");
                    return;
                }

                global::Android.Util.Log.Info("VirtualNanny", $"Download started: {url}, mimetype: {mimetype ?? "unknown"}, size: {contentLength}");

                // Extract filename from content disposition or URL
                string filename = GetFilenameFromContentDisposition(contentDisposition) 
                                ?? GetFilenameFromUrl(url) 
                                ?? $"recording_{DateTime.Now:yyyyMMdd_HHmmss}";

                // Ensure proper extension
                if (!filename.Contains('.'))
                {
                    string extension = GetExtensionFromMimeType(mimetype) ?? "webm";
                    filename += $".{extension}";
                }

                global::Android.Util.Log.Info("VirtualNanny", $"Using filename: {filename}");

                // For blob URLs, we need to handle differently
                if (url.StartsWith("blob:"))
                {
                    global::Android.Util.Log.Info("VirtualNanny", "Blob URL detected - cannot download directly");
                    // Blob URLs are handled by JavaScript download mechanism
                    return;
                }

                // Create download request
                var request = new DownloadManager.Request(global::Android.Net.Uri.Parse(url));
                request.SetMimeType(mimetype ?? "application/octet-stream");
                request.AddRequestHeader("User-Agent", userAgent ?? "VirtualNanny");
                request.SetDescription("VirtualNanny Recording");
                request.SetTitle(filename);

                // Only call AllowScanningByMediaScanner for older Android versions
                if (Build.VERSION.SdkInt < BuildVersionCodes.Q)
                {
                    request.AllowScanningByMediaScanner();
                }

                request.SetNotificationVisibility(DownloadVisibility.VisibleNotifyCompleted);

                // Save to Downloads directory
                request.SetDestinationInExternalPublicDir(global::Android.OS.Environment.DirectoryDownloads, filename);

                // Start download
                var context = Platform.CurrentActivity ?? global::Android.App.Application.Context;
                var downloadManager = context.GetSystemService(Context.DownloadService) as DownloadManager;
                downloadManager?.Enqueue(request);

                global::Android.Util.Log.Info("VirtualNanny", "Download enqueued successfully");
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("VirtualNanny", $"Download error: {ex.Message}");
            }
        }

        private string? GetFilenameFromContentDisposition(string? contentDisposition)
        {
            if (string.IsNullOrEmpty(contentDisposition))
                return null;

            var index = contentDisposition.IndexOf("filename=", StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var filename = contentDisposition.Substring(index + 9);
                return filename.Trim(' ', '"', '\'');
            }
            return null;
        }

        private string? GetFilenameFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var segments = uri.Segments;
                return segments.Length > 0 ? segments[^1] : null;
            }
            catch
            {
                return null;
            }
        }

        private string? GetExtensionFromMimeType(string? mimeType)
        {
            return mimeType switch
            {
                "video/webm" => "webm",
                "video/mp4" => "mp4",
                "video/avi" => "avi",
                "video/mov" => "mov",
                _ => "webm"
            };
        }
    }

    public class CameraWebChromeClient : WebChromeClient
    {
        // Public request code so MainActivity can handle the response
        public static int CAMERA_PERMISSION_REQUEST_CODE = 2002;

        // Keep a pending PermissionRequest while Android runtime permission is requested
        public static PermissionRequest? PendingPermissionRequest { get; set; }

        public override void OnPermissionRequest(PermissionRequest? request)
        {
            try
            {
                global::Android.Util.Log.Info("VirtualNanny", "WebView permission request received");

                if (request == null)
                {
                    global::Android.Util.Log.Warn("VirtualNanny", "Permission request is null");
                    return;
                }

                var resources = request.GetResources();
                if (resources != null)
                {
                    var grantedPermissions = new List<string>();
                    var neededAndroidPermissions = new List<string>();

                    foreach (var resource in resources)
                    {
                        global::Android.Util.Log.Info("VirtualNanny", $"WebView requested permission: {resource}");

                        // Map WebView resource requests to Android runtime permissions
                        if (resource == PermissionRequest.ResourceVideoCapture)
                        {
                            // Camera required
                            if (ContextCompat.CheckSelfPermission(Platform.CurrentActivity, global::Android.Manifest.Permission.Camera) == global::Android.Content.PM.Permission.Granted)
                            {
                                grantedPermissions.Add(resource);
                                global::Android.Util.Log.Info("VirtualNanny", "Camera permission already granted at OS level");
                            }
                            else
                            {
                                neededAndroidPermissions.Add(global::Android.Manifest.Permission.Camera);
                            }
                        }
                        else if (resource == PermissionRequest.ResourceAudioCapture)
                        {
                            // Microphone required
                            if (ContextCompat.CheckSelfPermission(Platform.CurrentActivity, global::Android.Manifest.Permission.RecordAudio) == global::Android.Content.PM.Permission.Granted)
                            {
                                grantedPermissions.Add(resource);
                                global::Android.Util.Log.Info("VirtualNanny", "Microphone permission already granted at OS level");
                            }
                            else
                            {
                                neededAndroidPermissions.Add(global::Android.Manifest.Permission.RecordAudio);
                            }
                        }
                        else
                        {
                            global::Android.Util.Log.Info("VirtualNanny", $"Ignoring other permission resource: {resource}");
                        }
                    }

                    if (neededAndroidPermissions.Count == 0 && grantedPermissions.Count > 0)
                    {
                        // All required Android permissions are already granted, grant to WebView
                        request.Grant(grantedPermissions.ToArray());
                        global::Android.Util.Log.Info("VirtualNanny", $"Granted WebView permissions: {string.Join(", ", grantedPermissions)}");
                        return;
                    }

                    if (neededAndroidPermissions.Count > 0)
                    {
                        // Store pending request and ask Android runtime permissions
                        PendingPermissionRequest = request;

                        var activity = Platform.CurrentActivity as global::Android.App.Activity;
                        if (activity != null)
                        {
                            global::Android.Util.Log.Info("VirtualNanny", $"Requesting Android runtime permissions: {string.Join(", ", neededAndroidPermissions)}");
                            ActivityCompat.RequestPermissions(activity, neededAndroidPermissions.ToArray(), CAMERA_PERMISSION_REQUEST_CODE);
                        }
                        else
                        {
                            global::Android.Util.Log.Warn("VirtualNanny", "Could not get current Activity to request permissions - denying WebView request");
                            request.Deny();
                        }

                        return;
                    }
                }

                // For other permissions or nothing matched, deny
                global::Android.Util.Log.Warn("VirtualNanny", "Denying WebView permission request - no matching resources or unable to satisfy OS permissions");
                request.Deny();
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("VirtualNanny", $"Error handling permission request: {ex.Message}");
                request?.Deny();
            }
        }

        public override void OnPermissionRequestCanceled(PermissionRequest? request)
        {
            global::Android.Util.Log.Warn("VirtualNanny", "WebView permission request was canceled");
            if (PendingPermissionRequest == request)
                PendingPermissionRequest = null;

            base.OnPermissionRequestCanceled(request);
        }

        public override bool OnShowFileChooser(global::Android.Webkit.WebView? webView, IValueCallback? filePathCallback, FileChooserParams? fileChooserParams)
        {
            global::Android.Util.Log.Info("VirtualNanny", "File chooser requested");
            return base.OnShowFileChooser(webView, filePathCallback, fileChooserParams);
        }

        public override bool OnConsoleMessage(ConsoleMessage? consoleMessage)
        {
            if (consoleMessage != null)
            {
                // Simple console logging without accessing specific properties
                global::Android.Util.Log.Info("VirtualNanny", "WebView Console message received");
            }
            return base.OnConsoleMessage(consoleMessage);
        }

        public override void OnGeolocationPermissionsShowPrompt(string? origin, GeolocationPermissions.ICallback? callback)
        {
            global::Android.Util.Log.Info("VirtualNanny", $"Geolocation permission requested for: {origin}");
            callback?.Invoke(origin, true, false);
        }
    }
}