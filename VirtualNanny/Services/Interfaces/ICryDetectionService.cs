namespace VirtualNanny.Services.Interfaces;

/// <summary>
/// Serwis do detekcji p³aczu dziecka na podstawie audio.
/// Integruje siê z modelem ML i obs³uguje hysterezê detekcji.
/// </summary>
public interface ICryDetectionService
{
    /// <summary>
    /// Przetwórz próbkê audio w celu detekcji p³aczu.
    /// </summary>
    /// <param name="audioSamples">Próbka audio (PCM 16-bit, mono)</param>
    /// <param name="cancellationToken">Token anulowania</param>
    Task OnAudioFrameAsync(short[]? audioSamples, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resetuj stan detektora (np. przed now¹ sesj¹).
    /// </summary>
    Task ResetAsync();

    /// <summary>
    /// Bie¿¹cy status detekcji.
    /// </summary>
    bool IsCryDetected { get; }

    /// <summary>
    /// Pewnoœæ ostatniej detekcji (0.0–1.0).
    /// </summary>
    float LastConfidence { get; }

    /// <summary>
    /// Próg pewnoœci do wyzwolenia alarmu (0.0–1.0).
    /// </summary>
    float ConfidenceThreshold { get; set; }

    /// <summary>
    /// Zdarzenie wywo³ywane, gdy p³acz zostaje wykryty.
    /// </summary>
    event EventHandler<CryDetectedEventArgs>? CryDetected;

    /// <summary>
    /// Zdarzenie wywo³ywane, gdy p³acz siê skoñczy³.
    /// </summary>
    event EventHandler<CryStoppedEventArgs>? CryStopped;
}

/// <summary>
/// Argumenty zdarzenia detekcji p³aczu.
/// </summary>
public class CryDetectedEventArgs : EventArgs
{
    /// <summary>
    /// Pewnoœæ detekcji (0.0–1.0).
    /// </summary>
    public float Confidence { get; set; }

    /// <summary>
    /// Czas detekcji.
    /// </summary>
    public DateTime DetectionTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Argumenty zdarzenia zakoñczenia p³aczu.
/// </summary>
public class CryStoppedEventArgs : EventArgs
{
    /// <summary>
    /// Czas zakoñczenia detekcji.
    /// </summary>
    public DateTime StoppedTime { get; set; } = DateTime.UtcNow;
}
