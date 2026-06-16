using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Menú principal de la experiencia.
/// MODIFICADO: Añade sonidos a los botones.
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
