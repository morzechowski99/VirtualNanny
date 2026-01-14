using Microsoft.Extensions.Logging;
using VirtualNanny.Services.Interfaces;

namespace VirtualNanny.Services;

/// <summary>
/// Podstawowa implementacja serwisu nagrywania.
/// Zawiera logikê wspóln¹ dla wszystkich platform.
/// </summary>
public abstract class RecordingService : IRecordingService
{
    protected readonly ILogger<RecordingService> Logger;
    protected readonly string _recordingsDirectory;
    protected bool _isRecording;
    protected DateTime _recordingStartTime;
    protected List<byte[]> _videoFrameBuffer;
    protected List<short[]> _audioSampleBuffer;
    protected int _lastVideoWidth;
    protected int _lastVideoHeight;
    protected int _lastAudioSampleRate;
    protected int _lastAudioChannels;

    public virtual bool IsRecording => _isRecording;

    public virtual long RecordingDurationMs
    {
        get
        {
            if (!_isRecording)
                return 0;
            return (long)(DateTime.UtcNow - _recordingStartTime).TotalMilliseconds;
        }
    }

    protected RecordingService(ILogger<RecordingService> logger)
    {
        Logger = logger;
        _recordingsDirectory = Path.Combine(FileSystem.AppDataDirectory, "Recordings");
        _videoFrameBuffer = new List<byte[]>();
        _audioSampleBuffer = new List<short[]>();
        _isRecording = false;

        // Utwórz katalog, jeœli nie istnieje
        Directory.CreateDirectory(_recordingsDirectory);
    }

    public virtual async Task StartRecordingAsync(CancellationToken cancellationToken = default)
    {
        if (_isRecording)
        {
            Logger.LogWarning("Recording already in progress");
            return;
        }

        _isRecording = true;
        _recordingStartTime = DateTime.UtcNow;
        _videoFrameBuffer.Clear();
        _audioSampleBuffer.Clear();

        Logger.LogInformation("Recording started");
        await Task.CompletedTask;
    }

    public virtual async Task StopRecordingAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRecording)
        {
            Logger.LogWarning("No recording in progress");
            return;
        }

        _isRecording = false;
        Logger.LogInformation("Recording stopped");
        await Task.CompletedTask;
    }

    public virtual async Task AddVideoFrameAsync(byte[] frameData, int width, int height, CancellationToken cancellationToken = default)
    {
        if (!_isRecording || frameData == null || frameData.Length == 0)
            return;

        _lastVideoWidth = width;
        _lastVideoHeight = height;
        _videoFrameBuffer.Add(frameData);
        await Task.CompletedTask;
    }

    public virtual async Task AddAudioSampleAsync(short[] audioSamples, int sampleRate, int channels, CancellationToken cancellationToken = default)
    {
        if (!_isRecording || audioSamples == null || audioSamples.Length == 0)
            return;

        _lastAudioSampleRate = sampleRate;
        _lastAudioChannels = channels;
        _audioSampleBuffer.Add(audioSamples);
        await Task.CompletedTask;
    }

    public abstract Task<string> SaveClipAsync(string filename, CancellationToken cancellationToken = default);

    public virtual async Task<List<RecordingInfo>> GetRecordedClipsAsync(CancellationToken cancellationToken = default)
    {
        var recordings = new List<RecordingInfo>();

        try
        {
            if (!Directory.Exists(_recordingsDirectory))
                return recordings;

            var files = Directory.GetFiles(_recordingsDirectory, "*.mp4")
                .OrderByDescending(f => File.GetCreationTime(f))
                .ToList();

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                recordings.Add(new RecordingInfo
                {
                    Filename = fileInfo.Name,
                    FilePath = file,
                    CreatedAt = fileInfo.CreationTime,
                    FileSizeBytes = fileInfo.Length,
                    DurationSeconds = 0, // TODO: Extract duration from MP4 metadata
                    ContainsCryDetection = false // TODO: Check metadata
                });
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving recorded clips");
        }

        return recordings;
    }

    public virtual async Task DeleteClipAsync(string filename, CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = Path.Combine(_recordingsDirectory, filename);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Logger.LogInformation("Deleted recording: {Filename}", filename);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting clip: {Filename}", filename);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Helper do wyczyszczenia buforów.
    /// </summary>
    protected void ClearBuffers()
    {
        _videoFrameBuffer.Clear();
        _audioSampleBuffer.Clear();
    }
}
