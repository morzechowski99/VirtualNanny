using NWaves.Audio;
using NWaves.FeatureExtractors;
using NWaves.FeatureExtractors.Options;

namespace VirtualNanny.CryDetection.Core;

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

    /// <summary>
    /// Extracts MFCC features from an audio file.
    /// </summary>
    /// <param name="filePath">Path to the audio file (.wav)</param>
    /// <returns>Array of 13 MFCC coefficients representing the audio characteristics</returns>
    public float[] Extract(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open);
        var waveFile = new WaveFile(stream);
        
        // Use averaged channels (automatic stereo to mono conversion)
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