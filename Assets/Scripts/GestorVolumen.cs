using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Aplica el volumen guardado al iniciar cualquier escena.
/// Coloca este script en un GameObject en la escena principal
/// (o en el mismo GameObject que MenuPausa).
///
/// INSPECTOR:
///   audioMixer       → MixerPrincipal
///   parametroVolumen → "VolMaster"
///   volumenDefault   → 0.7
/// </summary>
public class GestorVolumen : MonoBehaviour
{
    [Header("── Audio Mixer ─────────────────────────")]
    public AudioMixer audioMixer;
    public string parametroVolumen = "VolMaster";

    [Header("── Volumen por defecto (0 a 1) ─────────")]
    [Range(0f, 1f)]
    public float volumenDefault = 0.7f;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        AplicarVolumenGuardado();
    }

    // ─────────────────────────────────────────────────────────────────────
    void AplicarVolumenGuardado()
    {
        if (audioMixer == null) return;

        if (!PlayerPrefs.HasKey(parametroVolumen))
            PlayerPrefs.SetFloat(parametroVolumen, volumenDefault);

        float lineal = PlayerPrefs.GetFloat(parametroVolumen, volumenDefault);
        lineal = Mathf.Clamp(lineal, 0f, 1f);

        float dB = lineal > 0.0001f ? Mathf.Log10(lineal) * 20f : -10f;
        audioMixer.SetFloat(parametroVolumen, dB);

        Debug.Log($"[GestorVolumen] Volumen aplicado: {lineal:F2} ({dB:F1} dB)");
    }
}
