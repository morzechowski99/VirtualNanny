namespace VirtualNanny.AudioAnalysis.Interfaces;

public interface IAudioAnalyzer
{
    /// <summary>
    /// Analizuje próbkê dŸwiêku i zwraca true, jeœli wykryto p³acz.
    /// </summary>
    /// <param name="audioSamples">Próbka dŸwiêku (16-bit PCM, mono).</param>
    /// <param name="threshold">Próg g³oœnoœci (0-32767).</param>
    /// <returns></returns>
    bool IsCryDetected(short[] audioSamples, short threshold = 10000);
}
