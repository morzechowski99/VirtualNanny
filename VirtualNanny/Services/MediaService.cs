using Microsoft.Extensions.Logging;
using VirtualNanny.Services.Interfaces;

namespace VirtualNanny.Services;

/// <summary>
/// Implementacja serwisu mediów (odtwarzacze audio).
/// Wrapper dla cross-platform audio playback.
/// </summary>
public abstract class MediaService : IMediaService
{
    protected readonly ILogger<MediaService> Logger;
    protected float _volume = 1.0f;
    protected bool _isPlaying;

    public event EventHandler<EventArgs>? PlaybackCompleted;
    public event EventHandler<MediaErrorEventArgs>? PlaybackError;

    public virtual bool IsPlaying => _isPlaying;

    public virtual float Volume
    {
        get => _volume;
        set => _volume = Math.Clamp(value, 0.0f, 1.0f);
    }

    protected MediaService(ILogger<MediaService> logger)
    {
        Logger = logger;
    }

    public abstract Task PlayAsync(string filePath, CancellationToken cancellationToken = default);

    public abstract Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Helper do wyzwolenia zdarzenia PlaybackCompleted.
    /// </summary>
    protected virtual void OnPlaybackCompleted()
    {
        _isPlaying = false;
        PlaybackCompleted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Helper do raportowania b³êdów odtwarzania.
    /// </summary>
    protected virtual void OnPlaybackError(string message, Exception? ex = null)
    {
        _isPlaying = false;
        Logger.LogError(ex, "Playback error: {Message}", message);
        PlaybackError?.Invoke(this, new MediaErrorEventArgs { Message = message, Exception = ex });
    }
}
