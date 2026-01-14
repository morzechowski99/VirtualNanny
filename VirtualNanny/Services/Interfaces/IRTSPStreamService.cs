namespace VirtualNanny.Services.Interfaces;

/// <summary>
/// Serwis do obs³ugi strumienia RTSP ze smart-kamery sieciowej.
/// Odpowiada za pobieranie, dekodowanie video i audio.
/// </summary>
public interface IRTSPStreamService
{
    /// <summary>
    /// Nawi¹¿ po³¹czenie z kamer¹ RTSP.
    /// </summary>
    /// <param name="rtspUrl">URL strumienia RTSP (np. rtsp://192.168.1.100:554/stream)</param>
    /// <param name="username">Opcjonalna nazwa u¿ytkownika</param>
    /// <param name="password">Opcjonalne has³o</param>
    /// <param name="cancellationToken">Token anulowania</param>
    Task ConnectAsync(string rtspUrl, string? username = null, string? password = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Roz³¹czy siê z kamer¹.
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Pobranie kolejnej klatki video w formacie raw (RGB lub YUV).
    /// </summary>
    /// <returns>Tablica bajtów reprezentuj¹ca klatkê lub null, jeœli brak danych</returns>
    Task<byte[]?> GetFrameAsync();

    /// <summary>
    /// Pobranie próbki audio (PCM 16-bit, mono) z buffera.
    /// </summary>
    /// <returns>Tablica próbek audio lub null, jeœli brak danych</returns>
    Task<short[]?> GetAudioSamplesAsync();

    /// <summary>
    /// Czy serwis jest po³¹czony z kamer¹?
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Wymiary rozdzielczoœci video (szerokoœæ, wysokoœæ).
    /// </summary>
    (int Width, int Height) FrameDimensions { get; }

    /// <summary>
    /// Szybkoœæ próbkowania audio (Hz).
    /// </summary>
    int AudioSampleRate { get; }

    /// <summary>
    /// Iloœæ kana³ów audio (1 = mono, 2 = stereo).
    /// </summary>
    int AudioChannels { get; }

    /// <summary>
    /// Zdarzenie wywo³ywane, gdy nowa klatka video jest dostêpna.
    /// </summary>
    event EventHandler<EventArgs>? FrameReceived;

    /// <summary>
    /// Zdarzenie wywo³ywane, gdy dostêpna jest próbka audio.
    /// </summary>
    event EventHandler<EventArgs>? AudioAvailable;

    /// <summary>
    /// Zdarzenie wywo³ywane, gdy b³¹d po³¹czenia.
    /// </summary>
    event EventHandler<StreamErrorEventArgs>? StreamError;
}

/// <summary>
/// Argumenty zdarzenia b³êdu strumienia.
/// </summary>
public class StreamErrorEventArgs : EventArgs
{
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
}
