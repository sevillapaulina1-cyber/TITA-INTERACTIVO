using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.Collections;

/// <summary>
/// Menú principal de la experiencia.
/// MODIFICADO: Añade sonidos a los botones.
/// MODIFICADO: Reproduce un video de intro antes de mostrar el menú.
///             El video termina solo o se puede saltar con click/tap.
/// </summary>
public class MenuInicio : MonoBehaviour
{
    [Header("── Escenas ──────────────────────────────")]
    [Tooltip("Nombre exacto de tu escena de cinemática de intro")]
    public string escenaCinematica = "Cinematica";

    [Header("── UI ──────────────────────────────────")]
    public Text textoTitulo;
    public Text textoSubtitulo;
    public Button botonIniciar;
    public Button botonSalir;

    [Header("── Panel negro para fade ───────────────")]
    public Image panelFade;

    [Header("── Contenido ───────────────────────────")]
    public string titulo = "Experiencia Interactiva";
    public string subtitulo = "Una historia sobre grooming";

    [Header("── Tiempos ─────────────────────────────")]
    public float duracionFadeIn = 0.8f;
    public float duracionFadeOut = 1.0f;

    // ── ▼ AUDIO ───────────────────────────────────────────────────────────
    [Header("── Audio UI ────────────────────────────")]
    [Tooltip("Componente SonidoUI en el Canvas (se busca automáticamente)")]
    public SonidoUI sonidoUI;
    // ── ▲ AUDIO ───────────────────────────────────────────────────────────

    // ── ▼ VIDEO INTRO (NUEVO) ─────────────────────────────────────────────
    [Header("── Video Intro ─────────────────────────")]
    [Tooltip("Componente VideoPlayer que reproduce el video de intro")]
    public VideoPlayer videoPlayer;

    [Tooltip("RawImage donde se proyecta el video (debe cubrir toda la pantalla)")]
    public RawImage rawImageVideo;

    [Tooltip("CanvasGroup del panel que contiene el RawImage del video")]
    public CanvasGroup panelVideo;

    [Tooltip("Botón opcional para saltar el video. Déjalo vacío si no quieres mostrarlo.")]
    public Button botonSaltar;

    [Tooltip("Tiempo de fade al entrar y salir del video (segundos)")]
    public float duracionFadeVideo = 0.6f;

    [Tooltip("CanvasGroup que agrupa título, subtítulo y botones. Se oculta mientras corre el video.")]
    public CanvasGroup panelMenuPrincipal;
    // ── ▲ VIDEO INTRO ─────────────────────────────────────────────────────

    bool _saltarVideo = false;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (textoTitulo != null) textoTitulo.text = titulo;
        if (textoSubtitulo != null) textoSubtitulo.text = subtitulo;

        // Buscar SonidoUI automáticamente
        if (sonidoUI == null)
            sonidoUI = FindAnyObjectByType<SonidoUI>();

        if (botonIniciar != null)
        {
            botonIniciar.onClick.RemoveAllListeners();
            botonIniciar.onClick.AddListener(IniciarExperiencia);
            if (sonidoUI != null) sonidoUI.RegistrarBoton(botonIniciar, SonidoUI.TipoSonidoBtn.Click);
        }

        if (botonSalir != null)
        {
            botonSalir.onClick.RemoveAllListeners();
            botonSalir.onClick.AddListener(Salir);
            if (sonidoUI != null) sonidoUI.RegistrarBoton(botonSalir, SonidoUI.TipoSonidoBtn.Click);
        }

        // Botón saltar: activo sólo durante el video
        if (botonSaltar != null)
        {
            botonSaltar.onClick.RemoveAllListeners();
            botonSaltar.onClick.AddListener(SaltarVideo);
            botonSaltar.gameObject.SetActive(false);
        }

        // Ocultar menú principal mientras corre el video
        if (panelMenuPrincipal != null)
        {
            panelMenuPrincipal.alpha = 0f;
            panelMenuPrincipal.interactable = false;
            panelMenuPrincipal.blocksRaycasts = false;
        }

        // Si hay VideoPlayer y panel asignados arrancamos con el video; si no, directo al menú
        if (videoPlayer != null && panelVideo != null)
            StartCoroutine(SecuenciaVideoCO());
        else
            StartCoroutine(MostrarMenuCO());
    }

    // ─────────────────────────────────────────────────────────────────────
    // ── ▼ VIDEO INTRO ─────────────────────────────────────────────────────

    IEnumerator SecuenciaVideoCO()
    {
        _saltarVideo = false;

        // Panel de video empieza invisible
        panelVideo.alpha = 0f;
        panelVideo.interactable = true;
        panelVideo.blocksRaycasts = true;
        panelVideo.gameObject.SetActive(true);

        // Panel negro cubre la escena mientras preparamos el video
        SetAlpha(1f);

        // Preparar VideoPlayer y esperar a que esté listo
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared)
            yield return null;

        // Fade: negro → video visible
        videoPlayer.Play();
        yield return FadeCanvasGroup(panelVideo, 0f, 1f, duracionFadeVideo);
        yield return Fade(1f, 0f, duracionFadeVideo); // quitar panel negro

        // Mostrar botón saltar
        if (botonSaltar != null) botonSaltar.gameObject.SetActive(true);

        // Esperar a que el video termine o el jugador lo salte con click/tap
        while (videoPlayer.isPlaying && !_saltarVideo)
        {
            if (Input.GetMouseButtonDown(0) ||
               (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                _saltarVideo = true;

            yield return null;
        }

        videoPlayer.Stop();
        if (botonSaltar != null) botonSaltar.gameObject.SetActive(false);

        // Fade: video → negro → menú
        yield return Fade(0f, 1f, duracionFadeVideo);
        yield return FadeCanvasGroup(panelVideo, 1f, 0f, duracionFadeVideo);
        panelVideo.gameObject.SetActive(false);

        yield return MostrarMenuCO();
    }

    /// <summary>Llamado por el botón "Saltar" o puede invocarse por código.</summary>
    public void SaltarVideo() => _saltarVideo = true;

    // ── ▲ VIDEO INTRO ─────────────────────────────────────────────────────

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>Revela el panel del menú, inicia música y corre el fade-in habitual.</summary>
    IEnumerator MostrarMenuCO()
    {
        if (panelMenuPrincipal != null)
        {
            panelMenuPrincipal.alpha = 1f;
            panelMenuPrincipal.interactable = true;
            panelMenuPrincipal.blocksRaycasts = true;
        }

        // Iniciar música del menú (igual que antes, pero ahora va aquí)
        if (AudioManager.Instance != null)
            AudioManager.Instance.IniciarMusicaMenu();

        yield return FadeEntradaCO();
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator FadeEntradaCO()
    {
        if (panelFade == null) yield break;
        SetAlpha(1f);
        yield return Fade(1f, 0f, duracionFadeIn);
    }

    // ─────────────────────────────────────────────────────────────────────
    public void IniciarExperiencia()
    {
        StartCoroutine(IniciarCO());
    }

    IEnumerator IniciarCO()
    {
        if (botonIniciar != null) botonIniciar.interactable = false;
        if (botonSalir != null) botonSalir.interactable = false;

        yield return Fade(0f, 1f, duracionFadeOut);

        if (GameManager.Instance != null)
            GameManager.Instance.Reiniciar();

        // Detener música del menú antes de la cinemática
        if (AudioManager.Instance != null)
            AudioManager.Instance.DetenerMusicaMenu(0.3f);

        SceneManager.LoadScene(escenaCinematica);
    }

    // ─────────────────────────────────────────────────────────────────────
    public void Salir()
    {
        StartCoroutine(SalirCO());
    }

    IEnumerator SalirCO()
    {
        if (botonIniciar != null) botonIniciar.interactable = false;
        if (botonSalir != null) botonSalir.interactable = false;

        yield return Fade(0f, 1f, duracionFadeOut);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        Debug.Log("[MenuInicio] Salir.");
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator Fade(float desde, float hasta, float duracion)
    {
        if (panelFade == null) yield break;
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Lerp(desde, hasta, t / duracion));
            yield return null;
        }
        SetAlpha(hasta);
    }

    void SetAlpha(float a)
    {
        if (panelFade == null) return;
        Color c = panelFade.color;
        c.a = a;
        panelFade.color = c;
    }

    IEnumerator FadeCanvasGroup(CanvasGroup grupo, float desde, float hasta, float duracion)
    {
        if (grupo == null) yield break;
        grupo.alpha = desde;
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            grupo.alpha = Mathf.Lerp(desde, hasta, t / duracion);
            yield return null;
        }
        grupo.alpha = hasta;
    }
}
