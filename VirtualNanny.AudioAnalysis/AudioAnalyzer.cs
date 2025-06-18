using VirtualNanny.AudioAnalysis.Interfaces;

namespace VirtualNanny.AudioAnalysis;

public class AudioAnalyzer : IAudioAnalyzer
{
    public bool IsCryDetected(short[] audioSamples, short threshold = 10000)
        => audioSamples.Any(sample => Math.Abs(sample) > threshold);
}