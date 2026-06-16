using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Añade sonidos a los botones de la UI (menú inicio, pausa, skip, reiniciar, etc.)
///
/// USO 1 — Componente en el Canvas (registra botones automáticamente):
///   Pon SonidoUI en el Canvas de la escena y asigna los clips.
///   Llama a RegistrarBoton(boton) desde MenuInicio, MenuPausa, etc.
///
/// USO 2 — Estático desde cualquier script:
///   SonidoUI.TocarClick();
///   SonidoUI.TocarHover();
///
/// SETUP EN UNITY:
/// ──────────────────────────────────────────────────────────────────
/// Canvas
///   └── SonidoUI.cs
///         ├── fuenteUI        → AudioSource (no loop, no playOnAwake, spatialBlend 0)
///         ├── clipClick       → sonido de botón presionado
///         ├── clipHover       → sonido de hover (opcional)
///         ├── clipSkip        → sonido botón Skip/Saltar
///         ├── clipReiniciar   → sonido botón Reiniciar
///         └── volumen         → 0.8
/// ──────────────────────────────────────────────────────────────────
/// </summary>
public class SonidoUI : MonoBehaviour
{
    public static SonidoUI Instance { get; private set; }

    [Header("── Fuente de audio ──────────────────────")]
    public AudioSource fuenteUI;

    [Header("── Clips ────────────────────────────────")]
    [Tooltip("Sonido genérico de botón presionado")]
    public AudioClip clipClick;
    [Tooltip("Sonido de hover / foco en botón (opcional)")]
    public AudioClip clipHover;
    [Tooltip("Sonido específico para botones Skip / Saltar")]
    public AudioClip clipSkip;
    [Tooltip("Sonido específico para botón Reiniciar")]
    public AudioClip clipReiniciar;

    [Header("── Volumen ──────────────────────────────")]
    [Range(0f, 1f)]
    public float volumen = 0.8f;
    [Range(0f, 1f)]
    public float volumenHover = 0.5f;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        // Singleton débil — no persiste entre escenas (es por Canvas)
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        if (fuenteUI == null)
        {
            fuenteUI = gameObject.AddComponent<AudioSource>();
            fuenteUI.playOnAwake = false;
            fuenteUI.loop = false;
            fuenteUI.spatialBlend = 0f;
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Registra un botón para que suene al hacer click.
    /// Llama esto desde Awake/Start de MenuInicio, MenuPausa, etc.
    /// </summary>
    public void RegistrarBoton(Button boton, TipoSonidoBtn tipo = TipoSonidoBtn.Click)
    {
        if (boton == null) return;
        boton.onClick.AddListener(() => TocarSegunTipo(tipo));
    }

    public enum TipoSonidoBtn { Click, Skip, Reiniciar }

    void TocarSegunTipo(TipoSonidoBtn tipo)
    {
        switch (tipo)
        {
            case TipoSonidoBtn.Click:      TocarClick();      break;
            case TipoSonidoBtn.Skip:       TocarSkip();       break;
            case TipoSonidoBtn.Reiniciar:  TocarReiniciar();  break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Métodos estáticos — accesibles desde cualquier script
    public static void TocarClick()
    {
        if (Instance != null) Instance.Reproducir(Instance.clipClick, Instance.volumen);
    }

    public static void TocarHover()
    {
        if (Instance != null) Instance.Reproducir(Instance.clipHover, Instance.volumenHover);
    }

    public static void TocarSkip()
    {
        if (Instance != null)
        {
            AudioClip clip = Instance.clipSkip != null ? Instance.clipSkip : Instance.clipClick;
            Instance.Reproducir(clip, Instance.volumen);
        }
    }

    public static void TocarReiniciar()
    {
        if (Instance != null)
        {
            AudioClip clip = Instance.clipReiniciar != null ? Instance.clipReiniciar : Instance.clipClick;
            Instance.Reproducir(clip, Instance.volumen);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    void Reproducir(AudioClip clip, float vol)
    {
        if (clip == null || fuenteUI == null) return;
        fuenteUI.pitch = 1f;
        fuenteUI.PlayOneShot(clip, vol);
    }
}
