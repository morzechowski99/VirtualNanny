using Microsoft.Extensions.Logging;
using VirtualNanny.Services.Interfaces;

namespace VirtualNanny.Services;

/// <summary>
/// Podstawowa implementacja serwisu RTSP.
/// Zawiera logikê wspóln¹ dla wszystkich platform.
/// </summary>
public abstract class RTSPStreamService : IRTSPStreamService
{
    protected readonly ILogger<RTSPStreamService> Logger;
    protected string? _currentRtspUrl;
    protected string? _username;
    protected string? _password;
    protected int _frameWidth = 640;
    protected int _frameHeight = 480;
    protected int _audioSampleRate = 16000;
    protected int _audioChannels = 1;
    protected bool _isConnected;

    public event EventHandler<EventArgs>? FrameReceived;
    public event EventHandler<EventArgs>? AudioAvailable;
    public event EventHandler<StreamErrorEventArgs>? StreamError;

    protected RTSPStreamService(ILogger<RTSPStreamService> logger)
    {
        Logger = logger;
    }

    public virtual bool IsConnected 
    { 
        get => _isConnected;
        protected set => _isConnected = value;
    }

    public virtual (int Width, int Height) FrameDimensions => (_frameWidth, _frameHeight);

    public virtual int AudioSampleRate => _audioSampleRate;

    public virtual int AudioChannels => _audioChannels;

    public abstract Task ConnectAsync(string rtspUrl, string? username = null, string? password = null, CancellationToken cancellationToken = default);

    public abstract Task DisconnectAsync();

    public abstract Task<byte[]?> GetFrameAsync();

    public abstract Task<short[]?> GetAudioSamplesAsync();

    /// <summary>
    /// Helper do wyzwolenia zdarzenia FrameReceived.
    /// </summary>
    protected virtual void OnFrameReceived()
    {
        FrameReceived?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Helper do wyzwolenia zdarzenia AudioAvailable.
    /// </summary>
    protected virtual void OnAudioAvailable()
    {
        AudioAvailable?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Helper do raportowania b³êdów strumienia.
    /// </summary>
    protected virtual void OnStreamError(string message, Exception? ex = null)
    {
        Logger.LogError(ex, "Stream error: {Message}", message);
        StreamError?.Invoke(this, new StreamErrorEventArgs { Message = message, Exception = ex });
    }
}
