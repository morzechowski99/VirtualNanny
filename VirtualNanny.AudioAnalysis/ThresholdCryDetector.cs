using VirtualNanny.AudioAnalysis.Interfaces;

namespace VirtualNanny.AudioAnalysis;

public class ThresholdCryDetector : ICryDetector
{
    private bool _disposed;

    public bool IsCryDetected(short[] audioSamples, short threshold = 10000)
        => audioSamples.Any(sample => Math.Abs(sample) > threshold);

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            // No managed or unmanaged resources to release in this implementation.
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}