namespace VirtualNanny.Services.Interfaces;

/// <summary>
/// Serwis do obs³ugi powiadomieñ (alerty dŸwiêkowe, wibracja, powiadomienia systemowe).
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Odtwórz alert dŸwiêkowy.
    /// </summary>
    /// <param name="cancellationToken">Token anulowania</param>
    Task PlayAlertAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Wyzwól wibracjê urz¹dzenia.
    /// </summary>
    /// <param name="durationMs">Czas wibracji w milisekundach</param>
    /// <param name="cancellationToken">Token anulowania</param>
    Task VibrateAsync(int durationMs = 500, CancellationToken cancellationToken = default);

    /// <summary>
    /// Wyœwietl powiadomienie systemowe.
    /// </summary>
    /// <param name="title">Tytu³ powiadomienia</param>
    /// <param name="message">Treœæ powiadomienia</param>
    /// <param name="cancellationToken">Token anulowania</param>
    Task ShowNotificationAsync(string title, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zaloguj zdarzenie do bazy danych.
    /// </summary>
    /// <param name="cryEvent">Zdarzenie do zalogowania</param>
    /// <param name="cancellationToken">Token anulowania</param>
    Task LogEventAsync(CryDetectionEvent cryEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobierz historiê zdarzeñ detekcji.
    /// </summary>
    /// <param name="from">Pocz¹tkowa data (opcjonalnie)</param>
    /// <param name="to">Koñcowa data (opcjonalnie)</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Lista zdarzeñ</returns>
    Task<List<CryDetectionEvent>> GetEventHistoryAsync(DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Wyczyœæ historiê zdarzeñ starszych ni¿ X dni.
    /// </summary>
    /// <param name="retentionDays">Liczba dni do zachowania</param>
    /// <param name="cancellationToken">Token anulowania</param>
    Task CleanupOldEventsAsync(int retentionDays, CancellationToken cancellationToken = default);
}

/// <summary>
/// Zdarzenie detekcji p³aczu zarejestrowane w bazie.
/// </summary>
public class CryDetectionEvent
{
    /// <summary>
    /// Identyfikator zdarzenia.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Czas zdarzenia.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Pewnoœæ detekcji (0.0–1.0).
    /// </summary>
    public float Confidence { get; set; }

    /// <summary>
    /// Czas trwania p³aczu (sekundy).
    /// </summary>
    public int DurationSeconds { get; set; }

    /// <summary>
    /// Œcie¿ka do powi¹zanego nagrania (opcjonalnie).
    /// </summary>
    public string? RelatedRecordingPath { get; set; }

    /// <summary>
    /// Notatka u¿ytkownika (opcjonalnie).
    /// </summary>
    public string? Notes { get; set; }
}
