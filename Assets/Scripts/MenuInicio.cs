using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Menú principal de la experiencia.
/// MODIFICADO: Añade sonidos a los botones.
/// MODIFICADO: Añade pantallas previas (audífonos / advertencia educativa) antes del menú.
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

    // ── ▼ AUDIO (NUEVO) ──────────────────────────────────────────────────
    [Header("── Audio UI ────────────────────────────")]
    [Tooltip("Componente SonidoUI en el Canvas (se busca automáticamente)")]
    public SonidoUI sonidoUI;
    // ── ▲ AUDIO ──────────────────────────────────────────────────────────

    // ── ▼ PANTALLAS PREVIAS (NUEVO) ───────────────────────────────────────
    [Header("── Pantalla: Audífonos ─────────────────")]
    [Tooltip("CanvasGroup del panel de recomendación de audífonos")]
    public CanvasGroup panelAudifonos;
    public Text textoAudifonos;
    [TextArea(2, 4)]
    public string mensajeAudifonos = "Para una mejor experiencia,\nte recomendamos usar audífonos.";
    [Tooltip("Cuánto tiempo se muestra esta pantalla si nadie hace click/tap")]
    public float duracionPanelAudifonos = 4f;

    [Header("── Pantalla: Advertencia educativa ─────")]
    [Tooltip("CanvasGroup del panel de advertencia sobre el contenido")]
    public CanvasGroup panelAdvertencia;
    public Text textoAdvertencia;
    [TextArea(3, 6)]
    public string mensajeAdvertencia =
        "El contenido y la historia de esta experiencia tienen\n" +
        "fines educativos e informativos.\n\n" +
        "Aborda el tema del grooming en línea con el objetivo\n" +
        "de generar conciencia y prevención.";
    [Tooltip("Cuánto tiempo se muestra esta pantalla si nadie hace click/tap")]
    public float duracionPanelAdvertencia = 6f;

    [Header("── Pantallas previas: ajustes generales ─")]
    [Tooltip("Contenedor del menú principal (título, subtítulo, botones). Se oculta mientras corren las pantallas previas.")]
    public CanvasGroup panelMenuPrincipal;
    [Tooltip("Duración del fade in/out entre pantallas previas")]
    public float duracionFadePantallasPrevias = 0.5f;
    [Tooltip("Texto que indica que se puede saltar la pantalla (opcional)")]
    public Text textoSaltar;

    bool _saltarPantallaActual = false;
    // ── ▲ PANTALLAS PREVIAS ────────────────────────────────────────────────

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
            // ── ▼ AUDIO: registrar sonido de click (NUEVO) ────────────────
            if (sonidoUI != null) sonidoUI.RegistrarBoton(botonIniciar, SonidoUI.TipoSonidoBtn.Click);
            // ── ▲ AUDIO ──────────────────────────────────────────────────
        }

        if (botonSalir != null)
        {
            botonSalir.onClick.RemoveAllListeners();
            botonSalir.onClick.AddListener(Salir);
            // ── ▼ AUDIO: registrar sonido de click (NUEVO) ────────────────
            if (sonidoUI != null) sonidoUI.RegistrarBoton(botonSalir, SonidoUI.TipoSonidoBtn.Click);
            // ── ▲ AUDIO ──────────────────────────────────────────────────
        }

        // ── ▼ PANTALLAS PREVIAS (NUEVO) ───────────────────────────────────
        if (textoAudifonos != null) textoAudifonos.text = mensajeAudifonos;
        if (textoAdvertencia != null) textoAdvertencia.text = mensajeAdvertencia;

        // El menú principal arranca oculto; las pantallas previas se encargan de revelarlo al final
        if (panelMenuPrincipal != null)
        {
            panelMenuPrincipal.alpha = 0f;
            panelMenuPrincipal.interactable = false;
            panelMenuPrincipal.blocksRaycasts = false;
        }

        if (panelAudifonos != null) { panelAudifonos.alpha = 0f; panelAudifonos.gameObject.SetActive(false); }
        if (panelAdvertencia != null) { panelAdvertencia.alpha = 0f; panelAdvertencia.gameObject.SetActive(false); }

        StartCoroutine(SecuenciaInicioCO());
        // ── ▲ PANTALLAS PREVIAS ────────────────────────────────────────────
    }

    // ─────────────────────────────────────────────────────────────────────
    // ── ▼ PANTALLAS PREVIAS (NUEVO) ───────────────────────────────────────
    /// <summary>
    /// Corre, en orden: pantalla de audífonos -> pantalla de advertencia -> menú principal.
    /// Cada pantalla se puede saltar con click/tap, o avanza sola tras su duración.
    /// </summary>
    IEnumerator SecuenciaInicioCO()
    {
        // Si el panel negro existe, asegurarnos que no esté tapando la vista al arrancar
        if (panelFade != null) SetAlpha(0f);

        if (panelAudifonos != null)
            yield return MostrarPantallaPreviaCO(panelAudifonos, duracionPanelAudifonos);

        if (panelAdvertencia != null)
            yield return MostrarPantallaPreviaCO(panelAdvertencia, duracionPanelAdvertencia);

        // Revelar el menú principal y correr su fade-in habitual
        if (panelMenuPrincipal != null)
        {
            panelMenuPrincipal.gameObject.SetActive(true);
            panelMenuPrincipal.interactable = true;
            panelMenuPrincipal.blocksRaycasts = true;
        }

        StartCoroutine(FadeEntradaCO());
    }

    /// <summary>
    /// Muestra un panel (fade in), espera hasta que pase el tiempo indicado o el jugador haga click/tap,
    /// y luego lo oculta (fade out).
    /// </summary>
    IEnumerator MostrarPantallaPreviaCO(CanvasGroup panel, float duracion)
    {
        _saltarPantallaActual = false;

        panel.gameObject.SetActive(true);
        panel.interactable = true;
        panel.blocksRaycasts = true;

        if (textoSaltar != null) textoSaltar.gameObject.SetActive(true);

        // Fade in
        yield return FadeCanvasGroup(panel, 0f, 1f, duracionFadePantallasPrevias);

        // Esperar el tiempo configurado o un click/tap para saltar
        float t = 0f;
        while (t < duracion && !_saltarPantallaActual)
        {
            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                _saltarPantallaActual = true;

            t += Time.deltaTime;
            yield return null;
        }

        if (textoSaltar != null) textoSaltar.gameObject.SetActive(false);

        // Fade out
        yield return FadeCanvasGroup(panel, 1f, 0f, duracionFadePantallasPrevias);

        panel.interactable = false;
        panel.blocksRaycasts = false;
        panel.gameObject.SetActive(false);
    }

    /// <summary>
    /// Click/tap genérico para saltar la pantalla previa actual.
    /// Puede asignarse al evento OnPointerClick de un EventTrigger en cada panel,
    /// o llamarse desde un Button invisible que cubra toda la pantalla.
    /// </summary>
    public void SaltarPantallaPrevia()
    {
        _saltarPantallaActual = true;
    }

    IEnumerator FadeCanvasGroup(CanvasGroup grupo, float desde, float hasta, float duracion)
    {
        if (grupo == null) yield break;
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            grupo.alpha = Mathf.Lerp(desde, hasta, t / duracion);
            yield return null;
        }
        grupo.alpha = hasta;
    }
    // ── ▲ PANTALLAS PREVIAS ────────────────────────────────────────────────

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
}
