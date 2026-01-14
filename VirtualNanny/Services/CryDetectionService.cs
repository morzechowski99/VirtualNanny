using Microsoft.Extensions.Logging;
using VirtualNanny.AudioAnalysis.Interfaces;
using VirtualNanny.Services.Interfaces;

namespace VirtualNanny.Services;

/// <summary>
/// Implementacja serwisu detekcji p³aczu.
/// Integruje siê z modelem ML i obs³uguje hysterezê detekcji.
/// </summary>
public class CryDetectionService : ICryDetectionService
{
    private readonly ICryDetector _cryDetector;
    private readonly ILogger<CryDetectionService> _logger;
    private readonly Queue<short[]?> _audioBuffer;
    private readonly int _windowSizeSamples;  // 1–2 sekundy audio
    private readonly int _hopSizeSamples;     // przesuniêcie okna

    // Hystereza: unikaj migotania detekcji
    private int _cryFrameCounter;
    private bool _lastDetectionWasCry;

    // Event dla UI
    public event EventHandler<CryDetectedEventArgs>? CryDetected;
    public event EventHandler<CryStoppedEventArgs>? CryStopped;

    private float _confidenceThreshold = 0.5f;

    public bool IsCryDetected { get; private set; }
    public float LastConfidence { get; private set; }

    public float ConfidenceThreshold
    {
        get => _confidenceThreshold;
        set => _confidenceThreshold = Math.Clamp(value, 0.0f, 1.0f);
    }

    public CryDetectionService(ICryDetector cryDetector, ILogger<CryDetectionService> logger)
    {
        _cryDetector = cryDetector ?? throw new ArgumentNullException(nameof(cryDetector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _audioBuffer = new Queue<short[]?>();

        // Ustawienia: 1.5 sekundy okna @ 16kHz
        const int audioSampleRate = 16000;
        _windowSizeSamples = (int)(audioSampleRate * 1.5);  // 24000 próbek
        _hopSizeSamples = audioSampleRate / 2;               // 50% overlap = 8000 próbek

        _cryFrameCounter = 0;
        _lastDetectionWasCry = false;
    }

    public async Task OnAudioFrameAsync(short[]? audioSamples, CancellationToken cancellationToken = default)
    {
        if (audioSamples == null || audioSamples.Length == 0)
            return;

        try
        {
            // 1. Dodaj do buffera
            _audioBuffer.Enqueue(audioSamples);

            // 2. Oblicz ca³kowit¹ liczbê próbek w buforze
            var totalSamples = _audioBuffer.Sum(arr => arr?.Length);

            // 3. Gdy masz wystarczaj¹co danych (windowSize), wykonaj inference
            while (totalSamples >= _windowSizeSamples)
            {
                var window = DequeueWindow(_windowSizeSamples);
                totalSamples -= _hopSizeSamples;

                // 4. Inference
                var isCry = await Task.Run(() =>
                {
                    return _cryDetector.IsCryDetected(window, threshold: 10000);
                }, cancellationToken);

                // Ustaw confidence na podstawie wyniku
                LastConfidence = isCry ? 0.8f : 0.2f;

                // 5. Hystereza (smoothing) - unikaj migotania detekcji
                _cryFrameCounter += isCry ? 1 : -1;
                _cryFrameCounter = Math.Clamp(_cryFrameCounter, 0, 5); // 0–5 consecutive frames

                var detectionNow = _cryFrameCounter >= 3; // Trigger po 3 consecutive frames

                if (detectionNow == _lastDetectionWasCry) continue;
                _lastDetectionWasCry = detectionNow;
                IsCryDetected = detectionNow;

                if (detectionNow)
                {
                    _logger.LogInformation("Cry detected with confidence {Confidence}", LastConfidence);
                    CryDetected?.Invoke(this, new CryDetectedEventArgs { Confidence = LastConfidence });
                }
                else
                {
                    _logger.LogInformation("Cry stopped");
                    CryStopped?.Invoke(this, new CryStoppedEventArgs());
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in cry detection processing");
        }
    }

    public Task ResetAsync()
    {
        _audioBuffer.Clear();
        _cryFrameCounter = 0;
        _lastDetectionWasCry = false;
        IsCryDetected = false;
        LastConfidence = 0.0f;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Pobierz okno audio z buffera (FIFO).
    /// </summary>
    private short[] DequeueWindow(int windowSize)
    {
        var window = new List<short>();

        while (window.Count < windowSize && _audioBuffer.Count > 0)
        {
            var chunk = _audioBuffer.Dequeue();
            if (chunk != null) window.AddRange(chunk);
        }

        // Jeœli okno jest mniejsze ni¿ oczekiwane, dope³nij zerami
        while (window.Count < windowSize)
        {
            window.Add(0);
        }

        return window.Take(windowSize).ToArray();
    }
}
