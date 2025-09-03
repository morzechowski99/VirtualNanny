using NWaves.Audio;
using NWaves.FeatureExtractors;
using NWaves.FeatureExtractors.Options;

namespace VirtualNanny.CryDetection.Training;

/// <summary>
/// Extracts MFCC features from audio files.
/// </summary>
public class MfccFeatureExtractor
{
    private readonly int _mfccSize;
    private readonly int _sampleRate;
    private readonly int _frameSize;

    public MfccFeatureExtractor(int sampleRate = 16000, int mfccSize = 13, int frameSize = 512)
    {
        _sampleRate = sampleRate;
        _mfccSize = mfccSize;
        _frameSize = frameSize;
    }

    public float[] Extract(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open);
        var waveFile = new WaveFile(stream);
        
        // Try to use averaged channels (stereo to mono conversion)
        // If not available, fall back to left channel
        var samples = waveFile[Channels.Average];
        
        var options = new MfccOptions
        {
            SamplingRate = _sampleRate,
            FeatureCount = _mfccSize,
            FrameSize = _frameSize
        };
        var mfccExtractor = new MfccExtractor(options);
        var mfccs = mfccExtractor.ComputeFrom(samples);
        var meanMfcc = new float[_mfccSize];
        for (var i = 0; i < _mfccSize; i++)
            meanMfcc[i] = mfccs.Average(v => v[i]);
        return meanMfcc;
    }
}