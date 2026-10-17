using UnityEngine;

/// <summary>
/// Soft limiter on the final mix. Sits on the AudioListener: when thunder, a storm bed, music and a break land together the
/// sum can pass full scale, which hard-clips into crackle. Above a knee the signal is rounded off instead of cut.
/// </summary>
[DisallowMultipleComponent]
public sealed class MasterLimiter : MonoBehaviour
{
    const float Knee = .72f;

    void OnAudioFilterRead(float[] data, int channels)
    {
        for (int i = 0; i < data.Length; i++)
        {
            float x = data[i];
            float a = x < 0 ? -x : x;
            if (a <= Knee) continue;
            // Smoothly approach 1.0: Knee + (1 - Knee) * tanh((a - Knee) / (1 - Knee)).
            float over = (a - Knee) / (1 - Knee);
            float shaped = Knee + (1 - Knee) * (float)System.Math.Tanh(over);
            data[i] = x < 0 ? -shaped : shaped;
        }
    }
}
