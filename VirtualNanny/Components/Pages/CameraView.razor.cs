using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using VirtualNanny.Services;

namespace VirtualNanny.Components.Pages;

public partial class CameraView : ComponentBase
{
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] private ISnackbar SnackBar { get; set; } = null!;
    [Inject] private IFileService FileService { get; set; } = null!;

    private bool _isPermissionGranted = false;
    private bool _isCameraInitialized = false;
    private bool _isRecording = false;
    private bool _useFrontCamera = false;
    private bool _isSwitchingCamera = false; // Add this to prevent multiple simultaneous switches

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await CheckCameraPermission();
        }
    }

    private async Task CheckCameraPermission()
    {
        try
        {
            await LogDebug("Checking camera permissions...");
            
#if ANDROID
            // Check both camera and microphone permissions on Android
            var cameraStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
            var micStatus = await Permissions.CheckStatusAsync<Permissions.Microphone>();
            
            await LogDebug($"Android camera permission status: {cameraStatus}");
            await LogDebug($"Android microphone permission status: {micStatus}");
            
            // Camera is required, microphone is optional
            if (cameraStatus == PermissionStatus.Granted)
            {
                _isPermissionGranted = true;
                StateHasChanged();
                await Task.Delay(100);
                await InitializeCamera();
            }
            else
            {
                await LogDebug("Camera permission not granted on Android - will need to request");
            }
#elif IOS || MACCATALYST
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            await LogDebug($"iOS camera permission status: {status}");
            if (status == PermissionStatus.Granted)
            {
                _isPermissionGranted = true;
                StateHasChanged();
                await Task.Delay(100);
                await InitializeCamera();
            }
#else
            await LogDebug("Desktop platform - assuming permissions granted");
            _isPermissionGranted = true;
            StateHasChanged();
            await Task.Delay(100);
            await InitializeCamera();
#endif
            StateHasChanged();
        }
        catch (Exception ex)
        {
            await LogDebug($"Error checking permissions: {ex.Message}");
            SnackBar.Add($"B³¹d sprawdzania uprawnieñ: {ex.Message}", Severity.Error);
            
            // For web/desktop, try to initialize camera anyway
            _isPermissionGranted = true;
            StateHasChanged();
            await Task.Delay(100);
            await InitializeCamera();
            StateHasChanged();
        }
    }

    private async Task RequestCameraPermission()
    {
        try
        {
            await LogDebug("Requesting camera permission...");
            
#if ANDROID
            // Request camera permission (required)
            var cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
            await LogDebug($"Camera permission request result: {cameraStatus}");
            
            // Request microphone permission (optional - for recording)
            var micStatus = await Permissions.RequestAsync<Permissions.Microphone>();
            await LogDebug($"Microphone permission request result: {micStatus}");
            
            _isPermissionGranted = cameraStatus == PermissionStatus.Granted;
            
            if (_isPermissionGranted)
            {
                if (micStatus == PermissionStatus.Granted)
                {
                    SnackBar.Add("Dostêp do kamery i mikrofonu przyznany", Severity.Success);
                }
                else
                {
                    SnackBar.Add("Dostêp do kamery przyznany (mikrofon opcjonalny)", Severity.Info);
                }
            }
            else
            {
                SnackBar.Add("Dostêp do kamery jest wymagany do dzia³ania aplikacji", Severity.Error);
            }
#elif IOS
            var status = await Permissions.RequestAsync<Permissions.Camera>();
            _isPermissionGranted = status == PermissionStatus.Granted;
            await LogDebug($"iOS permission request result: {status}");
#else
            _isPermissionGranted = true;
            await LogDebug("Desktop platform - permission granted");
#endif
            
            if (_isPermissionGranted)
            {
                await InitializeCamera();
            }
            else
            {
                SnackBar.Add("Dostêp do kamery zosta³ odrzucony", Severity.Error);
            }
            StateHasChanged();
        }
        catch (Exception ex)
        {
            await LogDebug($"Error requesting permissions: {ex.Message}");
            SnackBar.Add($"B³¹d ¿¹dania uprawnieñ: {ex.Message}", Severity.Error);
            
            // Fallback for platforms that don't support permissions
            _isPermissionGranted = true;
            await InitializeCamera();
            StateHasChanged();
        }
    }

    private async Task InitializeCamera()
    {
        try
        {
            await LogDebug($"Initializing camera (front: {_useFrontCamera})...");
            
            // Check if we're in a secure context (required for camera access)
            var isSecureContext = await JsRuntime.InvokeAsync<bool>("eval", "window.isSecureContext");
            await LogDebug($"Secure context: {isSecureContext}");
            
            if (!isSecureContext)
            {
                SnackBar.Add("Dostêp do kamery wymaga bezpiecznego po³¹czenia (HTTPS)", Severity.Warning);
            }
            
            // Give more time to the UI to render the video element
            StateHasChanged();
            await Task.Delay(500);
            
            // Additional check to ensure DOM is ready
            var domReady = await JsRuntime.InvokeAsync<string>("eval", "document.readyState");
            await LogDebug($"DOM ready state: {domReady}");
            
            var result = await JsRuntime.InvokeAsync<bool>("initializeCamera", _useFrontCamera);
            
            if (result)
            {
                _isCameraInitialized = true;
                SnackBar.Add("Kamera zosta³a uruchomiona", Severity.Success);
                await LogDebug("Camera initialized successfully");
            }
            else
            {
                throw new Exception("JavaScript function returned false");
            }
            
            StateHasChanged();
        }
        catch (Exception ex)
        {
            await LogDebug($"Camera initialization error: {ex.Message}");
            SnackBar.Add($"B³¹d inicjalizacji kamery: {ex.Message}", Severity.Error);
            _isCameraInitialized = false;
            StateHasChanged();
        }
    }

    private async Task ToggleRecording()
    {
        try
        {
            if (!_isCameraInitialized)
            {
                SnackBar.Add("Kamera nie jest zainicjalizowana", Severity.Warning);
                return;
            }

            await LogDebug($"Toggling recording (currently: {_isRecording})...");
            
            _isRecording = !_isRecording;
            await JsRuntime.InvokeVoidAsync("toggleRecording", _isRecording);
            
            var message = _isRecording ? "Rozpoczêto nagrywanie" : "Zatrzymano nagrywanie";
            var severity = _isRecording ? Severity.Info : Severity.Success;
            SnackBar.Add(message, severity);
            
            await LogDebug($"Recording toggled successfully (now: {_isRecording})");
            StateHasChanged();
        }
        catch (Exception ex)
        {
            await LogDebug($"Recording toggle error: {ex.Message}");
            SnackBar.Add($"B³¹d nagrywania: {ex.Message}", Severity.Error);
            _isRecording = false;
            StateHasChanged();
        }
    }

    private async Task SwitchCamera()
    {
        // Prevent multiple simultaneous switches
        if (_isSwitchingCamera || _isRecording)
        {
            if (_isRecording)
            {
                SnackBar.Add("Nie mo¿na prze³¹czaæ kamery podczas nagrywania", Severity.Warning);
            }
            else
            {
                SnackBar.Add("Kamera jest ju¿ prze³¹czana", Severity.Info);
            }
            return;
        }

        try
        {
            _isSwitchingCamera = true;
            await LogDebug($"Switching camera (currently front: {_useFrontCamera})...");
            
            var targetFrontCamera = !_useFrontCamera;
            
            // Set loading state
            _isCameraInitialized = false;
            StateHasChanged();
            
            var result = await JsRuntime.InvokeAsync<bool>("switchCamera", targetFrontCamera);
            
            if (result)
            {
                _useFrontCamera = targetFrontCamera;
                _isCameraInitialized = true;
                var cameraType = _useFrontCamera ? "przedni¹" : "tyln¹";
                SnackBar.Add($"Prze³¹czono na kamerê {cameraType}", Severity.Info);
                await LogDebug($"Camera switched successfully to {(_useFrontCamera ? "front" : "back")}");
            }
            else
            {
                _isCameraInitialized = true;
                SnackBar.Add("Nie uda³o siê prze³¹czyæ kamery", Severity.Error);
                await LogDebug("Camera switch failed");
            }
            
            StateHasChanged();
        }
        catch (Exception ex)
        {
            await LogDebug($"Camera switch error: {ex.Message}");
            SnackBar.Add($"B³¹d prze³¹czania kamery: {ex.Message}", Severity.Error);
            _isCameraInitialized = true;
            StateHasChanged();
        }
        finally
        {
            _isSwitchingCamera = false;
        }
    }

    private async Task LogDebug(string message)
    {
        try
        {
            await JsRuntime.InvokeVoidAsync("debugLog", message);
        }
        catch
        {
            // Ignore debug logging errors
        }
    }

    private async Task RetryInitialization()
    {
        await LogDebug("Retrying camera initialization...");
        _isCameraInitialized = false;
        _isRecording = false;
        StateHasChanged();
        
        await Task.Delay(500);
        await InitializeCamera();
    }

    private async Task ShowJavaScriptAlert()
    {
        try
        {
            // Show a simple alert to test JS connectivity
            await JsRuntime.InvokeVoidAsync("alert", "JavaScript dzia³a! SprawdŸ DevTools console dla logów kamery.");
        }
        catch (Exception ex)
        {
            SnackBar.Add($"JS Alert error: {ex.Message}", Severity.Error);
        }
    }

    private async Task ShowDebugStatus()
    {
        try
        {
            await JsRuntime.InvokeVoidAsync("showRecentLogs");
        }
        catch (Exception ex)
        {
            SnackBar.Add($"Debug status error: {ex.Message}", Severity.Error);
        }
    }

    private async Task ForcePermissionRequest()
    {
        try
        {
            var result = await JsRuntime.InvokeAsync<bool>("forcePermissionRequest");
            if (result)
            {
                SnackBar.Add("Uprawnienia przyznane! Spróbuj teraz inicjalizacji kamery.", Severity.Success);
                
                // Try to initialize camera after successful permission
                await Task.Delay(1000);
                await RetryInitialization();
            }
            else
            {
                SnackBar.Add("Nie uda³o siê uzyskaæ uprawnieñ", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            SnackBar.Add($"Error forcing permissions: {ex.Message}", Severity.Error);
        }
    }

    private async Task TestAllPermissions()
    {
        try
        {
            var result = await JsRuntime.InvokeAsync<bool>("testAllPermissionMethods");
            if (result)
            {
                SnackBar.Add("Jedna z metod zadzia³a³a! Spróbuj inicjalizacji kamery.", Severity.Success);
                await Task.Delay(1000);
                await RetryInitialization();
            }
            else
            {
                SnackBar.Add("¯adna metoda nie zadzia³a³a. SprawdŸ alert z wynikami.", Severity.Warning);
            }
        }
        catch (Exception ex)
        {
            SnackBar.Add($"Error testing permissions: {ex.Message}", Severity.Error);
        }
    }

    private async Task TriggerWebViewDialog()
    {
        try
        {
            var result = await JsRuntime.InvokeAsync<bool>("triggerWebViewPermissions");
            if (result)
            {
                SnackBar.Add("WebView dialog zadzia³a³! Kamera powinna byæ dostêpna.", Severity.Success);
                _isCameraInitialized = true;
                StateHasChanged();
            }
            else
            {
                SnackBar.Add("WebView dialog nie zadzia³a³.", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            SnackBar.Add($"Error triggering WebView dialog: {ex.Message}", Severity.Error);
        }
    }

    private async Task UltimatePermissionFix()
    {
        try
        {
            SnackBar.Add("Rozpoczynam ultimate permission fix...", Severity.Info);
            
            var result = await JsRuntime.InvokeAsync<bool>("ultimatePermissionFix");
            if (result)
            {
                SnackBar.Add("?? ULTIMATE FIX ZADZIA£A³! Spróbuj teraz kamery.", Severity.Success);
                await Task.Delay(1000);
                await RetryInitialization();
            }
            else
            {
                SnackBar.Add("Ultimate fix nie pomóg³. SprawdŸ logi w DevTools.", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            SnackBar.Add($"Ultimate fix error: {ex.Message}", Severity.Error);
        }
    }

    private async Task ShowAvailableCameras()
    {
        try
        {
            await JsRuntime.InvokeVoidAsync("showAvailableCameras");
        }
        catch (Exception ex)
        {
            SnackBar.Add($"Error showing cameras: {ex.Message}", Severity.Error);
        }
    }

    private async Task CheckRecordingSupport()
    {
        try
        {
            await JsRuntime.InvokeVoidAsync("checkRecordingSupport");
        }
        catch (Exception ex)
        {
            SnackBar.Add($"Error checking recording support: {ex.Message}", Severity.Error);
        }
    }

    private async Task TestRecording()
    {
        try
        {
            if (!_isCameraInitialized)
            {
                SnackBar.Add("Kamera musi byæ zainicjalizowana przed testem nagrywania", Severity.Warning);
                return;
            }

            SnackBar.Add("Rozpoczynam test nagrywania na 5 sekund...", Severity.Info);
            var result = await JsRuntime.InvokeAsync<bool>("testRecording", 5);
            
            if (result)
            {
                SnackBar.Add("Test nagrywania rozpoczêty - plik zostanie pobrany po 5 sekundach", Severity.Success);
            }
            else
            {
                SnackBar.Add("Test nagrywania nie powiód³ siê", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            SnackBar.Add($"B³¹d testu nagrywania: {ex.Message}", Severity.Error);
        }
    }

    private async Task CheckDownloadCapabilities()
    {
        try
        {
            await JsRuntime.InvokeVoidAsync("checkDownloadCapabilities");
        }
        catch (Exception ex)
        {
            SnackBar.Add($"B³¹d sprawdzania mo¿liwoœci pobierania: {ex.Message}", Severity.Error);
        }
    }

    private async Task EnhancedPermissionRequest()
    {
        try
        {
            SnackBar.Add("Uruchamiam ulepszone ¿¹danie uprawnieñ...", Severity.Info);
            var result = await JsRuntime.InvokeAsync<bool>("enhancedPermissionRequest");
            
            if (result)
            {
                SnackBar.Add("Ulepszone uprawnienia przyznane! Spróbuj teraz inicjalizacji kamery.", Severity.Success);
            }
            else
            {
                SnackBar.Add("Ulepszone ¿¹danie uprawnieñ nie powiod³o siê", Severity.Warning);
            }
        }
        catch (Exception ex)
        {
            SnackBar.Add($"B³¹d ulepszonego ¿¹dania uprawnieñ: {ex.Message}", Severity.Error);
        }
    }

    private async Task ForceWebViewPermissions()
    {
        try
        {
            SnackBar.Add("Wymuszam uprawnienia WebView...", Severity.Info);
            var result = await JsRuntime.InvokeAsync<bool>("forceWebViewPermissions");
            
            if (result)
            {
                SnackBar.Add("Uprawnienia WebView wymuszone! Spróbuj teraz kamery.", Severity.Success);
                
                // Automatically try to initialize camera after forcing permissions
                StateHasChanged();
                await Task.Delay(500);
                await RetryInitialization();
            }
            else
            {
                SnackBar.Add("Wymuszenie uprawnieñ WebView nie powiod³o siê", Severity.Warning);
            }
        }
        catch (Exception ex)
        {
            SnackBar.Add($"B³¹d wymuszania uprawnieñ WebView: {ex.Message}", Severity.Error);
        }
    }

    private async Task VerifyCameraStream()
    {
        try
        {
            await JsRuntime.InvokeVoidAsync("verifyCameraStream");
        }
        catch (Exception ex)
        {
            SnackBar.Add($"B³¹d weryfikacji streamu kamery: {ex.Message}", Severity.Error);
        }
    }

    private async Task OnSwitchValueChanged(bool value)
    {
        // Immediately update the UI value
        _useFrontCamera = value;
        StateHasChanged();
        
        // Handle the camera switch asynchronously without blocking
        _ = Task.Run(async () => await HandleCameraSwitchInBackground(value));
    }

    private async Task HandleCameraSwitchInBackground(bool value)
    {
        // Prevent changes during recording or switching
        if (_isRecording || _isSwitchingCamera)
        {
            // Revert the value and update UI
            await InvokeAsync(() => {
                _useFrontCamera = !value;
                
                if (_isRecording)
                {
                    SnackBar.Add("Nie mo¿na prze³¹czaæ kamery podczas nagrywania", Severity.Warning);
                }
                else
                {
                    SnackBar.Add("Kamera jest ju¿ prze³¹czana", Severity.Info);
                }
                
                StateHasChanged();
            });
            return;
        }

        try
        {
            await LogDebug($"Handling camera switch to: {value}");
            
            if (_isCameraInitialized)
            {
                await InvokeAsync(() => {
                    _isSwitchingCamera = true;
                    StateHasChanged();
                });
                
                var result = await JsRuntime.InvokeAsync<bool>("switchCamera", value);
                
                await InvokeAsync(() => {
                    if (result)
                    {
                        _isCameraInitialized = true;
                        var cameraType = value ? "przedni¹" : "tyln¹";
                        SnackBar.Add($"Prze³¹czono na kamerê {cameraType}", Severity.Info);
                    }
                    else
                    {
                        // Revert on failure
                        _useFrontCamera = !value;
                        _isCameraInitialized = true;
                        SnackBar.Add("Nie uda³o siê prze³¹czyæ kamery", Severity.Error);
                    }
                    
                    _isSwitchingCamera = false;
                    StateHasChanged();
                });
                
                await LogDebug($"Camera switch completed, result: {result}");
            }
            else
            {
                await LogDebug($"Camera not initialized, just updated preference to: {value}");
            }
        }
        catch (Exception ex)
        {
            await LogDebug($"Camera switch error: {ex.Message}");
            
            await InvokeAsync(() => {
                _useFrontCamera = !value;
                _isSwitchingCamera = false;
                SnackBar.Add($"B³¹d prze³¹czania kamery: {ex.Message}", Severity.Error);
                StateHasChanged();
            });
        }
    }

    [JSInvokable("SaveFileFromJavaScript")]
    public static async Task<bool> SaveFileFromJavaScript(string base64Data, string filename, string mimeType)
    {
        try
        {
            // For static context, we need to get service through Platform
#if ANDROID
            var context = Platform.CurrentActivity ?? global::Android.App.Application.Context;
            var fileService = new VirtualNanny.Platforms.Android.Services.AndroidFileService();
#elif WINDOWS
            var fileService = new VirtualNanny.Platforms.Windows.Services.WindowsFileService();
#elif IOS
            var fileService = new VirtualNanny.Platforms.iOS.Services.iOSFileService();
#elif MACCATALYST
            var fileService = new VirtualNanny.Platforms.MacCatalyst.Services.MacCatalystFileService();
#else
            IFileService? fileService = null;
#endif

            if (fileService == null)
            {
                System.Diagnostics.Debug.WriteLine("FileService not available for this platform");
                return false;
            }

            // Convert base64 to byte array
            var fileData = Convert.FromBase64String(base64Data);
            
            System.Diagnostics.Debug.WriteLine($"Saving file: {filename}, size: {fileData.Length} bytes");
            
            var result = await fileService.SaveFileAsync(fileData, filename, mimeType);
            
            System.Diagnostics.Debug.WriteLine($"File save result: {result}");
            
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in SaveFileFromJavaScript: {ex.Message}");
            return false;
        }
    }
}