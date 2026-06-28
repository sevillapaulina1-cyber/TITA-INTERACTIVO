using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Menú principal de la experiencia.
/// MODIFICADO: Añade botón de Créditos que abre un panel sin cambiar de escena.
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
    public Button botonCreditos;          // ← NUEVO
    public Button botonSalir;

    [Header("── Panel negro para fade ───────────────")]
    public Image panelFade;

    [Header("── Créditos ────────────────────────────")]
    [Tooltip("Arrastra aquí el GameObject raíz del Panel Créditos")]
    public PanelCreditos panelCreditos;   // ← NUEVO

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

    // GameObject raíz de los botones del menú (para ocultarlos al abrir créditos)
    // Si todos los botones están bajo un mismo panel, asigna ese GameObject aquí.
    // Si no, se ocultan/muestran individualmente con los métodos internos.

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (textoTitulo != null) textoTitulo.text = titulo;
        if (textoSubtitulo != null) textoSubtitulo.text = subtitulo;

        if (sonidoUI == null)
            sonidoUI = FindAnyObjectByType<SonidoUI>();

        if (panelCreditos == null)
            panelCreditos = FindAnyObjectByType<PanelCreditos>();

        if (botonIniciar != null)
        {
            botonIniciar.onClick.RemoveAllListeners();
            botonIniciar.onClick.AddListener(IniciarExperiencia);
            if (sonidoUI != null) sonidoUI.RegistrarBoton(botonIniciar, SonidoUI.TipoSonidoBtn.Click);
        }

        // ── BOTÓN CRÉDITOS ────────────────────────────────────────────────
        if (botonCreditos != null)
        {
            botonCreditos.onClick.RemoveAllListeners();
            botonCreditos.onClick.AddListener(AbrirCreditos);
            if (sonidoUI != null) sonidoUI.RegistrarBoton(botonCreditos, SonidoUI.TipoSonidoBtn.Click);
        }
        // ─────────────────────────────────────────────────────────────────

        if (botonSalir != null)
        {
            botonSalir.onClick.RemoveAllListeners();
            botonSalir.onClick.AddListener(Salir);
            if (sonidoUI != null) sonidoUI.RegistrarBoton(botonSalir, SonidoUI.TipoSonidoBtn.Click);
        }

        // Iniciar música del menú
        if (AudioManager.Instance != null)
            AudioManager.Instance.IniciarMusicaMenu();

        StartCoroutine(FadeEntradaCO());
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator FadeEntradaCO()
    {
        if (panelFade == null) yield break;
        SetAlpha(1f);
        yield return Fade(1f, 0f, duracionFadeIn);
    }

    // ─────────────────────────────────────────────────────────────────────
    // ── CRÉDITOS ──────────────────────────────────────────────────────────

    void AbrirCreditos()
    {
        StartCoroutine(AbrirCreditosCO());
    }

    IEnumerator AbrirCreditosCO()
    {
        SetBotonesInteractivos(false);

        yield return Fade(0f, 1f, duracionFadeOut * 0.5f);

        // Ocultar menú y mostrar créditos
        SetBotonesVisibles(false);

        if (panelCreditos != null)
            panelCreditos.Mostrar();        // PanelCreditos hace su propio fade de entrada

        yield return Fade(1f, 0f, duracionFadeOut * 0.5f);
    }

    /// <summary>
    /// Llamado por PanelCreditos cuando el usuario pulsa "Regresar".
    /// Reactiva los botones del menú.
    /// </summary>
    public void MostrarMenuDesdeCreditos()
    {
        SetBotonesVisibles(true);
        SetBotonesInteractivos(true);
    }

    // ─────────────────────────────────────────────────────────────────────
    void SetBotonesVisibles(bool visible)
    {
        if (textoTitulo != null) textoTitulo.gameObject.SetActive(visible);
        if (textoSubtitulo != null) textoSubtitulo.gameObject.SetActive(visible);
        if (botonIniciar != null) botonIniciar.gameObject.SetActive(visible);
        if (botonCreditos != null) botonCreditos.gameObject.SetActive(visible);
        if (botonSalir != null) botonSalir.gameObject.SetActive(visible);
    }

    void SetBotonesInteractivos(bool interactable)
    {
        if (botonIniciar != null) botonIniciar.interactable = interactable;
        if (botonCreditos != null) botonCreditos.interactable = interactable;
        if (botonSalir != null) botonSalir.interactable = interactable;
    }

    // ─────────────────────────────────────────────────────────────────────
    public void IniciarExperiencia()
    {
        StartCoroutine(IniciarCO());
    }

    IEnumerator IniciarCO()
    {
        SetBotonesInteractivos(false);

        yield return Fade(0f, 1f, duracionFadeOut);

        if (GameManager.Instance != null)
            GameManager.Instance.Reiniciar();

        if (AudioManager.Instance != null)
            AudioManager.Instance.DetenerMusicaMenu(0.3f);

        // Ocultar cursor antes de cargar la cinemática
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SceneManager.LoadScene(escenaCinematica);
    }

    // ─────────────────────────────────────────────────────────────────────
    public void Salir()
    {
        StartCoroutine(SalirCO());
    }

    IEnumerator SalirCO()
    {
        SetBotonesInteractivos(false);

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
