using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace VirtualNanny.Components.Pages;

public partial class CameraView : ComponentBase
{
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] private ISnackbar SnackBar { get; set; } = null!;

    private bool _isPermissionGranted = false;
    private bool _isCameraInitialized = false;
    private bool _isRecording = false;
    private bool _useFrontCamera = false;

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
#if ANDROID
            // Suppress CA1416 by restricting to Android platform
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status == PermissionStatus.Granted)
            {
                _isPermissionGranted = true;
                await InitializeCamera();
            }
#elif IOS || MACCATALYST
            // Suppress CA1416 by restricting to iOS and MacCatalyst platforms
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status == PermissionStatus.Granted)
            {
                _isPermissionGranted = true;
                await InitializeCamera();
            }
#else
            // For other platforms, assume permission is granted
            _isPermissionGranted = true;
            await InitializeCamera();
#endif
            StateHasChanged();
        }
        catch (Exception ex)
        {
            SnackBar.Add($"B³¹d sprawdzania uprawnieñ: {ex.Message}", Severity.Error);
            // For web/desktop, try to initialize camera anyway
            _isPermissionGranted = true;
            await InitializeCamera();
            StateHasChanged();
        }
    }

    private async Task RequestCameraPermission()
    {
        try
        {
#if ANDROID || IOS
            var status = await Permissions.RequestAsync<Permissions.Camera>();
            _isPermissionGranted = status == PermissionStatus.Granted;
#else
            _isPermissionGranted = true;
#endif
            
            if (_isPermissionGranted)
            {
                SnackBar.Add("Dostêp do kamery przyznany", Severity.Success);
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
            await JsRuntime.InvokeVoidAsync("initializeCamera", _useFrontCamera);
            _isCameraInitialized = true;
            SnackBar.Add("Kamera zosta³a uruchomiona", Severity.Success);
            StateHasChanged();
        }
        catch (Exception ex)
        {
            SnackBar.Add($"B³¹d inicjalizacji kamery: {ex.Message}", Severity.Error);
        }
    }

    private async Task ToggleRecording()
    {
        try
        {
            _isRecording = !_isRecording;
            await JsRuntime.InvokeVoidAsync("toggleRecording", _isRecording);
            
            var message = _isRecording ? "Rozpoczêto nagrywanie" : "Zatrzymano nagrywanie";
            var severity = _isRecording ? Severity.Info : Severity.Success;
            SnackBar.Add(message, severity);
            
            StateHasChanged();
        }
        catch (Exception ex)
        {
            SnackBar.Add($"B³¹d nagrywania: {ex.Message}", Severity.Error);
        }
    }

    private async Task SwitchCamera()
    {
        try
        {
            _useFrontCamera = !_useFrontCamera;
            await JsRuntime.InvokeVoidAsync("switchCamera", _useFrontCamera);
            
            var cameraType = _useFrontCamera ? "przedni¹" : "tyln¹";
            SnackBar.Add($"Prze³¹czono na kamerê {cameraType}", Severity.Info);
        }
        catch (Exception ex)
        {
            SnackBar.Add($"B³¹d prze³¹czania kamery: {ex.Message}", Severity.Error);
        }
    }
}