namespace VirtualNanny.AudioAnalysis.Interfaces;

public interface IAudioAnalyzer
{
    /// <summary>
    /// Analizuje pr�bk� d�wi�ku i zwraca true, je�li wykryto p�acz.
    /// </summary>
    /// <param name="audioSamples">Pr�bka d�wi�ku (16-bit PCM, mono).</param>
    /// <param name="threshold">Pr�g g�o�no�ci (0-32767).</param>
    /// <returns></returns>
    bool IsCryDetected(short[] audioSamples, short threshold = 10000);
}
