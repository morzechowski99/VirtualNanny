using Microsoft.Extensions.Logging;
using VirtualNanny.Services.Interfaces;

namespace VirtualNanny.Services;

/// <summary>
/// Implementacja serwisu powiadomieñ.
/// Obs³uguje alerty, wibracje, powiadomienia systemowe i logowanie zdarzeñ.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly IMediaService _mediaService;
    private string _alertSoundPath = string.Empty;

    // Placeholder dla bazy danych (TODO: zintegrowaæ z SQLite)
    private readonly List<CryDetectionEvent> _eventHistory = [];

    public NotificationService(ILogger<NotificationService> logger, IMediaService mediaService)
    {
        _logger = logger;
        _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
    }

    public async Task PlayAlertAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // SprawdŸ, czy plik dŸwiêku istnieje
            if (!string.IsNullOrEmpty(_alertSoundPath) && File.Exists(_alertSoundPath))
            {
                await _mediaService.PlayAsync(_alertSoundPath, cancellationToken);
            }
            else
            {
                _logger.LogWarning("Alert sound file not found: {Path}", _alertSoundPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error playing alert sound");
        }
    }

    public async Task VibrateAsync(int durationMs = 500, CancellationToken cancellationToken = default)
    {
        try
        {
            // MAUI Vibration API
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(durationMs));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Vibration not available on this platform");
        }

        await Task.CompletedTask;
    }

    public async Task ShowNotificationAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Notification: {Title} - {Message}", title, message);

            // TODO: Zintegrowaæ z MAUI LocalNotificationService
            // Tutaj bêdzie wsparcie dla platform-specific notifications
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing notification");
        }

        await Task.CompletedTask;
    }

    public async Task LogEventAsync(CryDetectionEvent cryEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Zapisz do SQLite
            _eventHistory.Add(cryEvent);

            _logger.LogInformation(
                "Logged cry event: Confidence={Confidence}, Duration={DurationSeconds}s, RecordingPath={RecordingPath}",
                cryEvent.Confidence,
                cryEvent.DurationSeconds,
                cryEvent.RelatedRecordingPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging event");
        }

        await Task.CompletedTask;
    }

    public async Task<List<CryDetectionEvent>> GetEventHistoryAsync(DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _eventHistory.AsEnumerable();

            if (from.HasValue)
                query = query.Where(e => e.Timestamp >= from.Value);

            if (to.HasValue)
                query = query.Where(e => e.Timestamp <= to.Value);

            return await Task.FromResult(query.OrderByDescending(e => e.Timestamp).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving event history");
            return [];
        }
    }

    public async Task CleanupOldEventsAsync(int retentionDays, CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
            var removedCount = _eventHistory.RemoveAll(e => e.Timestamp < cutoffDate);

            _logger.LogInformation("Cleaned up {RemovedCount} old events (retention: {RetentionDays} days)", removedCount, retentionDays);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old events");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Ustaw œcie¿kê do pliku dŸwiêku alarmu.
    /// </summary>
    public void SetAlertSoundPath(string filePath)
    {
        _alertSoundPath = filePath;
    }
}
