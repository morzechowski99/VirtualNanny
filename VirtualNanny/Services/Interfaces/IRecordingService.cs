namespace VirtualNanny.Services.Interfaces;

/// <summary>
/// Serwis do obs³ugi nagrywania video i audio ze strumienia RTSP.
/// Zapisuje pliki w formacie MP4 lub oddzielnie video + audio.
/// </summary>
public interface IRecordingService
{
    /// <summary>
    /// Uruchomij nagrywanie.
    /// </summary>
    /// <param name="cancellationToken">Token anulowania</param>
    Task StartRecordingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Zatrzymaj nagrywanie.
    /// </summary>
    /// <param name="cancellationToken">Token anulowania</param>
    Task StopRecordingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Dodaj klatkê video do bie¿¹cego nagrania.
    /// </summary>
    /// <param name="frameData">Dane klatki (raw bytes)</param>
    /// <param name="width">Szerokoœæ klatki</param>
    /// <param name="height">Wysokoœæ klatki</param>
    /// <param name="cancellationToken">Token anulowania</param>
    Task AddVideoFrameAsync(byte[] frameData, int width, int height, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dodaj próbkê audio do bie¿¹cego nagrania.
    /// </summary>
    /// <param name="audioSamples">Próbka audio (PCM 16-bit)</param>
    /// <param name="sampleRate">Szybkoœæ próbkowania (Hz)</param>
    /// <param name="channels">Iloœæ kana³ów</param>
    /// <param name="cancellationToken">Token anulowania</param>
    Task AddAudioSampleAsync(short[] audioSamples, int sampleRate, int channels, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zapisz klip nagrania do pliku.
    /// </summary>
    /// <param name="filename">Nazwa pliku (bez œcie¿ki)</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Pe³na œcie¿ka do zapisanego pliku</returns>
    Task<string> SaveClipAsync(string filename, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobierz listê nagranych klipów.
    /// </summary>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Lista nagrañ z metadanymi</returns>
    Task<List<RecordingInfo>> GetRecordedClipsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Usuñ nagrany klip.
    /// </summary>
    /// <param name="filename">Nazwa pliku do usuniêcia</param>
    /// <param name="cancellationToken">Token anulowania</param>
    Task DeleteClipAsync(string filename, CancellationToken cancellationToken = default);

    /// <summary>
    /// Czy nagrywanie jest aktywne?
    /// </summary>
    bool IsRecording { get; }

    /// <summary>
    /// Czas trwania bie¿¹cego nagrania (ms).
    /// </summary>
    long RecordingDurationMs { get; }
}

/// <summary>
/// Metadane nagrania.
/// </summary>
public class RecordingInfo
{
    /// <summary>
    /// Nazwa pliku.
    /// </summary>
    public string Filename { get; set; } = string.Empty;

    /// <summary>
    /// Pe³na œcie¿ka do pliku.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Czas utworzenia nagrania.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Czas trwania nagrania (s).
    /// </summary>
    public int DurationSeconds { get; set; }

    /// <summary>
    /// Rozmiar pliku (bytes).
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Czy nagranie zawiera detekcjê p³aczu?
    /// </summary>
    public bool ContainsCryDetection { get; set; }
}
