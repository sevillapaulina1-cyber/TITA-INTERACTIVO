using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton central de audio. Gestiona:
///   - Música de ambiente: Momentos 1–8 (musicaNormal) y Momentos 9–12 (musicaTension)
///   - La transición a musicaTension ocurre en el Momento 8 (animación de paneo)
///   - Silenciado durante transición de días
///   - Sonidos de respiración durante la animación del Momento 8
///   - Música de retroalimentación después de la cinemática final
///   - Todas las AudioSources pasan por AudioMixerGroups → controlables con el slider
///
/// SETUP EN UNITY:
/// ─────────────────────────────────────────────────────────────────
/// 1. Crea un GameObject "AudioManager" en la escena MenuInicio.
/// 2. Agrega 4 componentes AudioSource al mismo GameObject:
///      FuenteMusica, FuenteCrossfade, FuenteRespiracion, FuenteRetro
///    Todos con: Play On Awake OFF, Loop OFF
/// 3. Crea un Audio Mixer (Project → Create → Audio Mixer) llamado "MixerPrincipal":
///      Master
///        ├── Musica    ← clic derecho → "Expose 'Volume' to script" → renombrar "VolMusica"
///        └── SFX       ← clic derecho → "Expose 'Volume' to script" → renombrar "VolSFX"
/// 4. En el Inspector de AudioManager:
///      fuenteMusica      → el AudioSource principal    | Output → MixerGroup "Musica"
///      fuenteCrossfade   → el AudioSource de crossfade | Output → MixerGroup "Musica"
///      fuenteRespiracion → el AudioSource de respir.   | Output → MixerGroup "SFX"
///      fuenteRetro       → el AudioSource de retro     | Output → MixerGroup "Musica"
///      grupoMusica       → AudioMixerGroup "Musica"
///      grupoSFX          → AudioMixerGroup "SFX"
///      audioMixer        → el AudioMixer raíz (MixerPrincipal)
///      parametroMusica   → "VolMusica"
///      parametroSFX      → "VolSFX"
/// 5. En MenuPausa, el slider de volumen llama CambiarVolumen(float) como siempre,
///    pero ahora el AudioMixer del MenuPausa debe ser el mismo MixerPrincipal.
///    El parámetro que usa MenuPausa ("VolMaster") debe estar expuesto en Master.
///
/// ESCENAS DE FINAL (Final_1, Final_2):
///   Agrega UIRetroalimentacion y llama DetenerMusicaJuego() en Start → ya integrado.
/// ─────────────────────────────────────────────────────────────────
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("── Fuentes de audio ──────────────────────")]
    public AudioSource fuenteMusica;
    public AudioSource fuenteCrossfade;
    public AudioSource fuenteRespiracion;
    public AudioSource fuenteRetro;

    [Header("── AudioMixer (para slider de volumen) ────")]
    [Tooltip("AudioMixerGroup al que pertenece la música (Output de las AudioSources de música)")]
    public AudioMixerGroup grupoMusica;
    [Tooltip("AudioMixerGroup para SFX/respiración")]
    public AudioMixerGroup grupoSFX;
    [Tooltip("El AudioMixer raíz — mismo que usa MenuPausa")]
    public AudioMixer audioMixer;
    [Tooltip("Nombre del parámetro expuesto de volumen de música en el Mixer")]
    public string parametroMusica = "VolMusica";
    [Tooltip("Nombre del parámetro expuesto de volumen SFX en el Mixer")]
    public string parametroSFX = "VolSFX";

    [Header("── Clips de música ───────────────────────")]
    [Tooltip("Música ambiente para momentos 1–7")]
    public AudioClip musicaNormal;
    [Tooltip("Música de tensión para momentos 8–12 (empieza en la animación del M8)")]
    public AudioClip musicaTension;
    [Tooltip("Música para la pantalla de retroalimentación final")]
    public AudioClip musicaRetroalimentacion;
    [Tooltip("Música para el menú de inicio")]
    public AudioClip musicaMenu;

    [Header("── Clips de efectos ──────────────────────")]
    [Tooltip("Sonido de respiración durante la animación del Momento 8")]
    public AudioClip sonidoRespiracion;

    [Header("── Tiempos ─────────────────────────────")]
    public float duracionCrossfade = 2.0f;
    public float duracionFadeTransicion = 0.8f;

    // ── Estado interno ────────────────────────────────────────────────────
    bool _enTension = false;
    bool _silenciado = false;
    bool _musicaActiva = false;  // true solo en la escena de juego
    bool _modoRetro = false;  // true en escenas de final
    bool _enAnimacionM8 = false;  // true durante el paneo del M8
    bool _modoMenu = false;  // true en MenuInicio
    Coroutine _coroutineCross;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        AsignarMixerGroups();
        SceneManager.sceneLoaded += OnSceneCargada;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneCargada;
    }

    // Detecta cuando se carga una escena de final para detener la música del juego
    void OnSceneCargada(Scene escena, LoadSceneMode modo)
    {
        // Si es una escena que NO es la principal del juego, detener música de juego
        // Las escenas de final son las que contienen UIRetroalimentacion —
        // pero lo más seguro es apagar si ya no hay GameManager activo con juego en curso.
        bool esEscenaFinal = escena.name.Contains("Final") || escena.name.Contains("final");
        if (esEscenaFinal)
        {
            DetenerMusicaJuego();
        }
    }

    void AsignarMixerGroups()
    {
        if (grupoMusica != null)
        {
            if (fuenteMusica != null) fuenteMusica.outputAudioMixerGroup = grupoMusica;
            if (fuenteCrossfade != null) fuenteCrossfade.outputAudioMixerGroup = grupoMusica;
            if (fuenteRetro != null) fuenteRetro.outputAudioMixerGroup = grupoMusica;
        }
        if (grupoSFX != null)
        {
            if (fuenteRespiracion != null) fuenteRespiracion.outputAudioMixerGroup = grupoSFX;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        // El Update solo controla la transición automática de M9+ en la escena de juego.
        // La transición al Momento 8 (animación) se hace manualmente desde SistemaDialogo.
        if (!_musicaActiva || _silenciado || _modoRetro || _enAnimacionM8) return;
        if (GameManager.Instance == null) return;

        int momento = GameManager.Instance.MomentoActual;

        // Después del M8 (momento 9+) mantener tensión si no estamos ya en ella
        if (momento >= 9 && !_enTension)
        {
            _enTension = true;
            CambiarMusica(musicaTension);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Inicia la música normal del juego. Llamar desde InicializadorAudio en EscenaPrincipal.
    /// </summary>
    public void IniciarMusicaJuego()
    {
        if (fuenteMusica == null || musicaNormal == null) return;

        _enTension = false;
        _silenciado = false;
        _modoRetro = false;
        _modoMenu = false;
        _musicaActiva = true;
        _enAnimacionM8 = false;

        // Detener retro si venía de un reinicio
        if (fuenteRetro != null && fuenteRetro.isPlaying)
        {
            fuenteRetro.Stop();
            fuenteRetro.volume = 0f;
        }

        if (fuenteMusica.isPlaying) fuenteMusica.Stop();
        fuenteMusica.clip = musicaNormal;
        fuenteMusica.loop = true;
        fuenteMusica.volume = 1f;
        fuenteMusica.Play();
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Inicia la música del menú de inicio.
    /// Llamar desde MenuInicio.Start().
    /// </summary>
    public void IniciarMusicaMenu()
    {
        if (fuenteMusica == null || musicaMenu == null) return;

        _modoMenu = true;
        _musicaActiva = false;
        _modoRetro = false;
        _enTension = false;
        _silenciado = false;
        _enAnimacionM8 = false;

        if (fuenteMusica.isPlaying) fuenteMusica.Stop();
        fuenteMusica.clip = musicaMenu;
        fuenteMusica.loop = true;
        fuenteMusica.volume = 1f;
        fuenteMusica.Play();
    }

    /// <summary>
    /// Detiene la música del menú con fade out (para la cinemática).
    /// Llamar desde MenuInicio antes de cargar la escena de cinemática.
    /// </summary>
    public void DetenerMusicaMenu(float duracion = 0.8f)
    {
        _modoMenu = false;
        if (_coroutineCross != null) { StopCoroutine(_coroutineCross); _coroutineCross = null; }
        StartCoroutine(FadeVolumenCO(fuenteMusica, fuenteMusica != null ? fuenteMusica.volume : 0f, 0f, duracion));
        StartCoroutine(FadeVolumenCO(fuenteCrossfade, fuenteCrossfade != null ? fuenteCrossfade.volume : 0f, 0f, duracion));
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Crossfade hacia una nueva pista de música.
    /// </summary>
    public void CambiarMusica(AudioClip nuevoClip)
    {
        if (nuevoClip == null) return;
        if (fuenteMusica != null && fuenteMusica.clip == nuevoClip && fuenteMusica.isPlaying) return;

        if (_coroutineCross != null) StopCoroutine(_coroutineCross);
        _coroutineCross = StartCoroutine(CrossfadeCO(nuevoClip));
    }

    IEnumerator CrossfadeCO(AudioClip nuevoClip)
    {
        // Pasar pista actual a la fuente secundaria
        if (fuenteCrossfade != null && fuenteMusica != null && fuenteMusica.clip != null)
        {
            fuenteCrossfade.clip = fuenteMusica.clip;
            fuenteCrossfade.volume = fuenteMusica.volume;
            fuenteCrossfade.loop = true;
            fuenteCrossfade.Play();
        }

        // Nueva pista en la fuente principal (volumen 0)
        if (fuenteMusica != null)
        {
            fuenteMusica.clip = nuevoClip;
            fuenteMusica.volume = 0f;
            fuenteMusica.loop = true;
            fuenteMusica.Play();
        }

        float t = 0f;
        while (t < duracionCrossfade)
        {
            t += Time.deltaTime;
            float p = t / duracionCrossfade;
            if (fuenteMusica != null) fuenteMusica.volume = Mathf.Lerp(0f, 1f, p);
            if (fuenteCrossfade != null) fuenteCrossfade.volume = Mathf.Lerp(1f, 0f, p);
            yield return null;
        }

        if (fuenteMusica != null) fuenteMusica.volume = 1f;
        if (fuenteCrossfade != null) { fuenteCrossfade.Stop(); fuenteCrossfade.clip = null; }
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Silencia gradualmente la música. Llamar al iniciar una transición de días.
    /// </summary>
    public void SilenciarParaTransicion()
    {
        if (_silenciado || !_musicaActiva) return;
        _silenciado = true;

        if (_coroutineCross != null) { StopCoroutine(_coroutineCross); _coroutineCross = null; }

        StartCoroutine(FadeVolumenCO(fuenteMusica, fuenteMusica != null ? fuenteMusica.volume : 0f, 0f, duracionFadeTransicion));
        StartCoroutine(FadeVolumenCO(fuenteCrossfade, fuenteCrossfade != null ? fuenteCrossfade.volume : 0f, 0f, duracionFadeTransicion));
    }

    /// <summary>
    /// Restaura la música después de una transición de días.
    /// </summary>
    public void RestaurarMusica()
    {
        if (!_silenciado) return;
        _silenciado = false;

        if (fuenteMusica != null && !fuenteMusica.isPlaying && fuenteMusica.clip != null)
            fuenteMusica.Play();

        StartCoroutine(FadeVolumenCO(fuenteMusica, 0f, 1f, duracionFadeTransicion));
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Llama esto al INICIO de la animación del Momento 8.
    /// Apaga la música normal y arranca la de tensión + respiración.
    /// </summary>
    public void IniciarAnimacionMomento8()
    {
        if (!_musicaActiva) return;
        _enAnimacionM8 = true;
        _enTension = true;

        // Silenciar música normal con crossfade hacia tensión
        CambiarMusica(musicaTension);

        // Iniciar respiración
        IniciarRespiracion();
    }

    /// <summary>
    /// Llama esto al TERMINAR la animación del Momento 8.
    /// Detiene la respiración; la música de tensión continúa.
    /// </summary>
    public void TerminarAnimacionMomento8()
    {
        _enAnimacionM8 = false;
        DetenerRespiracion();
        // La música de tensión sigue sonando — Update la mantendrá para M9+
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>Inicia el sonido de respiración (loop con fade in).</summary>
    public void IniciarRespiracion()
    {
        if (fuenteRespiracion == null || sonidoRespiracion == null) return;
        fuenteRespiracion.clip = sonidoRespiracion;
        fuenteRespiracion.loop = true;
        fuenteRespiracion.volume = 0f;
        fuenteRespiracion.Play();
        StartCoroutine(FadeVolumenCO(fuenteRespiracion, 0f, 1f, 1.5f));
    }

    /// <summary>Detiene el sonido de respiración con fade out.</summary>
    public void DetenerRespiracion()
    {
        if (fuenteRespiracion == null || !fuenteRespiracion.isPlaying) return;
        StartCoroutine(FadeYDetenerCO(fuenteRespiracion, 1.0f));
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Detiene la música del juego al entrar a una escena de final.
    /// Se llama automáticamente desde OnSceneCargada y también desde UIRetroalimentacion.
    /// </summary>
    public void DetenerMusicaJuego()
    {
        _musicaActiva = false;
        _modoRetro = true;
        _enAnimacionM8 = false;

        if (_coroutineCross != null) { StopCoroutine(_coroutineCross); _coroutineCross = null; }

        StartCoroutine(FadeVolumenCO(fuenteMusica, fuenteMusica != null ? fuenteMusica.volume : 0f, 0f, 0.5f));
        StartCoroutine(FadeVolumenCO(fuenteCrossfade, fuenteCrossfade != null ? fuenteCrossfade.volume : 0f, 0f, 0.5f));
        StartCoroutine(FadeVolumenCO(fuenteRespiracion, fuenteRespiracion != null ? fuenteRespiracion.volume : 0f, 0f, 0.5f));
    }

    /// <summary>
    /// Inicia la música de retroalimentación (después del video final).
    /// Llamar desde UIRetroalimentacion.MostrarPantallaRetro().
    /// </summary>
    public void IniciarMusicaRetro()
    {
        if (fuenteRetro == null || musicaRetroalimentacion == null) return;

        // Asegurarse de que la música de juego esté detenida
        if (fuenteMusica != null && fuenteMusica.isPlaying) fuenteMusica.Stop();
        if (fuenteCrossfade != null && fuenteCrossfade.isPlaying) fuenteCrossfade.Stop();

        fuenteRetro.clip = musicaRetroalimentacion;
        fuenteRetro.loop = true;
        fuenteRetro.volume = 0f;
        fuenteRetro.Play();
        StartCoroutine(FadeVolumenCO(fuenteRetro, 0f, 1f, 2.0f));
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator FadeVolumenCO(AudioSource fuente, float desde, float hasta, float duracion)
    {
        if (fuente == null) yield break;
        fuente.volume = desde;
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            fuente.volume = Mathf.Lerp(desde, hasta, t / duracion);
            yield return null;
        }
        fuente.volume = hasta;
        if (hasta <= 0f && fuente.isPlaying) fuente.Stop();
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