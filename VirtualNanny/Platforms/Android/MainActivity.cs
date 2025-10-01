using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using VirtualNanny.Platforms.Android;
using Android.Webkit;

namespace VirtualNanny
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, 
              ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        private const int STORAGE_PERMISSION_REQUEST_CODE = 1001;
        private static readonly int CAMERA_AND_AUDIO_PERMISSION_REQUEST_CODE = CameraWebChromeClient.CAMERA_PERMISSION_REQUEST_CODE;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Enable WebView debugging in debug mode
#if DEBUG
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
            {
                Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
            }
#endif

            // Request storage permissions for downloads
            RequestStoragePermissions();

            // Request camera/microphone at startup to avoid black screen in WebView
            RequestCameraAndAudioPermissions();
        }

        private void RequestStoragePermissions()
        {
            var permissions = new List<string>();

            // For Android 13+ (API 33+), use new media permissions
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            {
                if (ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.ReadMediaVideo) != Permission.Granted)
                {
                    permissions.Add(Android.Manifest.Permission.ReadMediaVideo);
                }
            }
            else
            {
                // For older Android versions
                if (ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.WriteExternalStorage) != Permission.Granted)
                {
                    permissions.Add(Android.Manifest.Permission.WriteExternalStorage);
                }
                if (ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.ReadExternalStorage) != Permission.Granted)
                {
                    permissions.Add(Android.Manifest.Permission.ReadExternalStorage);
                }
            }

            if (permissions.Count > 0)
            {
                Android.Util.Log.Info("VirtualNanny", $"Requesting storage permissions: {string.Join(", ", permissions)}");
                ActivityCompat.RequestPermissions(this, permissions.ToArray(), STORAGE_PERMISSION_REQUEST_CODE);
            }
            else
            {
                Android.Util.Log.Info("VirtualNanny", "Storage permissions already granted");
            }
        }

        private void RequestCameraAndAudioPermissions()
        {
            var permissions = new List<string>();

            if (ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.Camera) != Permission.Granted)
            {
                permissions.Add(Android.Manifest.Permission.Camera);
            }

            if (ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.RecordAudio) != Permission.Granted)
            {
                permissions.Add(Android.Manifest.Permission.RecordAudio);
            }

            if (permissions.Count > 0)
            {
                Android.Util.Log.Info("VirtualNanny", $"Requesting camera/audio permissions: {string.Join(", ", permissions)}");
                ActivityCompat.RequestPermissions(this, permissions.ToArray(), CAMERA_AND_AUDIO_PERMISSION_REQUEST_CODE);
            }
            else
            {
                Android.Util.Log.Info("VirtualNanny", "Camera and audio permissions already granted");
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (requestCode == STORAGE_PERMISSION_REQUEST_CODE)
            {
                for (int i = 0; i < permissions.Length; i++)
                {
                    var permission = permissions[i];
                    var result = grantResults[i];

                    Android.Util.Log.Info("VirtualNanny", $"Permission {permission}: {result}");

                    if (result == Permission.Granted)
                    {
                        Android.Util.Log.Info("VirtualNanny", $"Storage permission {permission} granted");
                    }
                    else
                    {
                        Android.Util.Log.Warn("VirtualNanny", $"Storage permission {permission} denied");
                    }
                }
            }
            else if (requestCode == CAMERA_AND_AUDIO_PERMISSION_REQUEST_CODE)
            {
                var anyGranted = false;

                for (int i = 0; i < permissions.Length; i++)
                {
                    var permission = permissions[i];
                    var result = grantResults[i];
                    Android.Util.Log.Info("VirtualNanny", $"Camera/Audio Permission {permission}: {result}");

                    if (result == Permission.Granted)
                    {
                        anyGranted = true;
                    }
                }

                // If we have granted at least one, and there's a pending WebView permission request, grant the WebView one accordingly
                if (anyGranted && CameraWebChromeClient.PendingPermissionRequest != null)
                {
                    try
                    {
                        var pending = CameraWebChromeClient.PendingPermissionRequest;
                        var resources = pending.GetResources();
                        var toGrant = new List<string>();

                        foreach (var res in resources)
                        {
                            if (res == PermissionRequest.ResourceVideoCapture && ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.Camera) == Permission.Granted)
                                toGrant.Add(res);

                            if (res == PermissionRequest.ResourceAudioCapture && ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.RecordAudio) == Permission.Granted)
                                toGrant.Add(res);
                        }

                        if (toGrant.Count > 0)
                        {
                            pending.Grant(toGrant.ToArray());
                            Android.Util.Log.Info("VirtualNanny", $"Granted pending WebView permissions: {string.Join(", ", toGrant)}");
                        }
                        else
                        {
                            pending.Deny();
                            Android.Util.Log.Warn("VirtualNanny", "Pending WebView permission denied because OS permissions not granted");
                        }
                    }
                    catch (Exception ex)
                    {
                        Android.Util.Log.Error("VirtualNanny", $"Error granting pending WebView permissions: {ex.Message}");
                        CameraWebChromeClient.PendingPermissionRequest?.Deny();
                    }
                    finally
                    {
                        CameraWebChromeClient.PendingPermissionRequest = null;
                    }
                }
            }
        }
    }
}
