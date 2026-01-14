namespace VirtualNanny.Services.Interfaces;

/// <summary>
/// Serwis do abstrakcji odtwarzacza mediów.
/// Wrapper dla cross-platform audio playback.
/// </summary>
public interface IMediaService
{
    /// <summary>
    /// Odtwórz plik audio ze œcie¿ki.
    /// </summary>
    /// <param name="filePath">Pe³na œcie¿ka do pliku audio</param>
    /// <param name="cancellationToken">Token anulowania</param>
    Task PlayAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zatrzymaj bie¿¹ce odtwarzanie.
    /// </summary>
    /// <param name="cancellationToken">Token anulowania</param>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Czy odtwarzanie jest w toku?
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// G³oœnoœæ (0.0–1.0).
    /// </summary>
    float Volume { get; set; }

    /// <summary>
    /// Zdarzenie wywo³ywane, gdy odtwarzanie siê skoñczy.
    /// </summary>
    event EventHandler<EventArgs>? PlaybackCompleted;

    /// <summary>
    /// Zdarzenie wywo³ywane, gdy b³¹d odtwarzania.
    /// </summary>
    event EventHandler<MediaErrorEventArgs>? PlaybackError;
}

/// <summary>
/// Argumenty zdarzenia b³êdu odtwarzania.
/// </summary>
public class MediaErrorEventArgs : EventArgs
{
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
}
