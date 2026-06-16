using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Singleton central de audio. Gestiona:
///   - Música de ambiente: Momentos 1–8 (musicaNormal) y Momentos 9–12 (musicaTension)
///   - Se silencia durante la transición de días (llamar SilenciarParaTransicion / RestaurarMusica)
///   - Sonidos de respiración durante la animación del Momento 8
///   - Música de retroalimentación final
///
/// SETUP EN UNITY:
/// ──────────────────────────────────────────────────────────────────
/// AudioManager  (GameObject vacío, persiste entre escenas)
///   └── AudioManager.cs
///         ├── fuenteMusica         → AudioSource  (loop, no playOnAwake)
///         ├── fuenteTransicion     → AudioSource  (loop, no playOnAwake) — para crossfade
///         ├── fuenteRespiracion    → AudioSource  (loop, no playOnAwake)
///         ├── fuenteRetro          → AudioSource  (loop, no playOnAwake)
///         ├── musicaNormal         → clip para momentos 1–8
///         ├── musicaTension        → clip para momentos 9–12
///         ├── musicaRetroalimentacion → clip para pantalla de retroalimentación
///         ├── sonidoRespiracion    → clip de respiración
///         ├── duracionCrossfade    → 2.0
///         └── audioMixer           → AudioMixer (opcional, para el slider de volumen)
/// ──────────────────────────────────────────────────────────────────
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("── Fuentes de audio ──────────────────────")]
    [Tooltip("Fuente principal de música (loop)")]
    public AudioSource fuenteMusica;
    [Tooltip("Fuente secundaria para crossfade (loop)")]
    public AudioSource fuenteTransicion;
    [Tooltip("Fuente de respiración (loop)")]
    public AudioSource fuenteRespiracion;
    [Tooltip("Fuente de música de retroalimentación (loop)")]
    public AudioSource fuenteRetro;

    [Header("── Clips de música ───────────────────────")]
    [Tooltip("Música para momentos 1–8 (ambiente tranquilo)")]
    public AudioClip musicaNormal;
    [Tooltip("Música para momentos 9–12 (tensión creciente)")]
    public AudioClip musicaTension;
    [Tooltip("Música para la pantalla de retroalimentación final")]
    public AudioClip musicaRetroalimentacion;

    [Header("── Clips de efectos ──────────────────────")]
    [Tooltip("Sonido de respiración durante animación del Momento 8")]
    public AudioClip sonidoRespiracion;

    [Header("── Tiempos ─────────────────────────────")]
    [Tooltip("Duración del crossfade entre pistas (segundos)")]
    public float duracionCrossfade = 2.0f;
    [Tooltip("Duración del fade de silenciado en transición de días")]
    public float duracionFadeTransicion = 0.8f;

    [Header("── Mixer (opcional) ────────────────────")]
    public AudioMixer audioMixer;

    // ── Estado ────────────────────────────────────────────────────────────
    bool _enTension = false;
    bool _silenciado = false;
    float _volumenAntes = 1f;
    Coroutine _coroutineActiva;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Iniciar música normal al arrancar
        if (fuenteMusica != null && musicaNormal != null)
        {
            fuenteMusica.clip = musicaNormal;
            fuenteMusica.loop = true;
            fuenteMusica.volume = 1f;
            fuenteMusica.Play();
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (GameManager.Instance == null) return;
        if (_silenciado) return;

        int momento = GameManager.Instance.MomentoActual;

        // Momento 9+ → cambiar a música de tensión
        if (momento >= 8 && !_enTension)
        {
            _enTension = true;
            CambiarMusica(musicaTension);
        }
        // Si por algún debug se vuelve a momentos anteriores
        else if (momento < 8 && _enTension)
        {
            _enTension = false;
            CambiarMusica(musicaNormal);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Crossfade entre la pista actual y una nueva.
    /// </summary>
    public void CambiarMusica(AudioClip nuevoClip)
    {
        if (nuevoClip == null) return;
        if (fuenteMusica != null && fuenteMusica.clip == nuevoClip && fuenteMusica.isPlaying) return;

        if (_coroutineActiva != null) StopCoroutine(_coroutineActiva);
        _coroutineActiva = StartCoroutine(CrossfadeCO(nuevoClip));
    }

    IEnumerator CrossfadeCO(AudioClip nuevoClip)
    {
        // Copiar la pista actual a la fuente de transición
        if (fuenteTransicion != null && fuenteMusica != null)
        {
            fuenteTransicion.clip = fuenteMusica.clip;
            fuenteTransicion.volume = fuenteMusica.volume;
            fuenteTransicion.loop = true;
            if (fuenteTransicion.clip != null) fuenteTransicion.Play();
        }

        // Preparar nueva pista en fuente principal (volumen 0)
        if (fuenteMusica != null)
        {
            fuenteMusica.clip = nuevoClip;
            fuenteMusica.volume = 0f;
            fuenteMusica.loop = true;
            fuenteMusica.Play();
        }

        // Crossfade
        float t = 0f;
        while (t < duracionCrossfade)
        {
            t += Time.deltaTime;
            float p = t / duracionCrossfade;
            if (fuenteMusica != null) fuenteMusica.volume = Mathf.Lerp(0f, 1f, p);
            if (fuenteTransicion != null) fuenteTransicion.volume = Mathf.Lerp(1f, 0f, p);
            yield return null;
        }

        if (fuenteMusica != null) fuenteMusica.volume = 1f;
        if (fuenteTransicion != null) { fuenteTransicion.Stop(); fuenteTransicion.clip = null; }
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Silencia la música de ambiente gradualmente (llamar al iniciar transición de días).
    /// </summary>
    public void SilenciarParaTransicion()
    {
        if (_silenciado) return;
        _silenciado = true;
        _volumenAntes = fuenteMusica != null ? fuenteMusica.volume : 1f;
        StartCoroutine(FadeVolumenCO(fuenteMusica, _volumenAntes, 0f, duracionFadeTransicion));
        if (fuenteTransicion != null)
            StartCoroutine(FadeVolumenCO(fuenteTransicion, fuenteTransicion.volume, 0f, duracionFadeTransicion));
    }

    /// <summary>
    /// Restaura la música de ambiente (llamar al terminar la transición de días).
    /// </summary>
    public void RestaurarMusica()
    {
        if (!_silenciado) return;
        _silenciado = false;
        StartCoroutine(FadeVolumenCO(fuenteMusica, 0f, 1f, duracionFadeTransicion));
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Inicia el sonido de respiración (durante animación Momento 8).
    /// </summary>
    public void IniciarRespiracion()
    {
        if (fuenteRespiracion == null || sonidoRespiracion == null) return;
        fuenteRespiracion.clip = sonidoRespiracion;
        fuenteRespiracion.loop = true;
        fuenteRespiracion.volume = 0f;
        fuenteRespiracion.Play();
        StartCoroutine(FadeVolumenCO(fuenteRespiracion, 0f, 1f, 1.5f));
    }

    /// <summary>
    /// Detiene el sonido de respiración con fade out.
    /// </summary>
    public void DetenerRespiracion()
    {
        if (fuenteRespiracion == null || !fuenteRespiracion.isPlaying) return;
        StartCoroutine(FadeYDetenerCO(fuenteRespiracion, 1.0f));
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Detiene la música de ambiente y reproduce la música de retroalimentación.
    /// Llamar desde UIRetroalimentacion al mostrar el panel.
    /// </summary>
    public void IniciarMusicaRetro()
    {
        // Detener ambiente
        if (fuenteMusica != null) StartCoroutine(FadeVolumenCO(fuenteMusica, fuenteMusica.volume, 0f, 1.5f));
        if (fuenteTransicion != null) StartCoroutine(FadeVolumenCO(fuenteTransicion, fuenteTransicion.volume, 0f, 1.5f));

        // Iniciar retro
        if (fuenteRetro != null && musicaRetroalimentacion != null)
        {
            fuenteRetro.clip = musicaRetroalimentacion;
            fuenteRetro.loop = true;
            fuenteRetro.volume = 0f;
            fuenteRetro.Play();
            StartCoroutine(FadeVolumenCO(fuenteRetro, 0f, 1f, 2.0f));
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator FadeVolumenCO(AudioSource fuente, float desde, float hasta, float duracion)
    {
        if (fuente == null) yield break;
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            fuente.volume = Mathf.Lerp(desde, hasta, t / duracion);
            yield return null;
        }
        fuente.volume = hasta;
    }

    IEnumerator FadeYDetenerCO(AudioSource fuente, float duracion)
    {
        float volInicial = fuente.volume;
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            fuente.volume = Mathf.Lerp(volInicial, 0f, t / duracion);
            yield return null;
        }
        fuente.Stop();
        fuente.volume = volInicial;
    }
}
