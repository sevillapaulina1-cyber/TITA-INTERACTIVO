using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Panel de créditos del menú principal.
/// Se activa/desactiva desde MenuInicio sin cambiar de escena.
/// Asigna este script al GameObject raíz del Panel Créditos.
/// </summary>
public class PanelCreditos : MonoBehaviour
{
    [Header("── Botón ────────────────────────────────")]
    public Button botonRegresar;

    [Header("── Fade ────────────────────────────────")]
    [Tooltip("Panel negro para el fade (puede ser el mismo de MenuInicio)")]
    public Image panelFade;
    public float duracionFade = 0.4f;

    // ── ▼ AUDIO ──────────────────────────────────────────────────────────
    [Header("── Audio UI ────────────────────────────")]
    public SonidoUI sonidoUI;
    // ── ▲ AUDIO ──────────────────────────────────────────────────────────

    // referencia inversa para poder volver a activar el menú
    MenuInicio menuInicio;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        // Empieza oculto
        gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (sonidoUI == null)
            sonidoUI = FindAnyObjectByType<SonidoUI>();

        menuInicio = FindAnyObjectByType<MenuInicio>();

        if (botonRegresar != null)
        {
            botonRegresar.onClick.RemoveAllListeners();
            botonRegresar.onClick.AddListener(Regresar);
            if (sonidoUI != null)
                sonidoUI.RegistrarBoton(botonRegresar, SonidoUI.TipoSonidoBtn.Click);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>Llamado desde MenuInicio al pulsar "Créditos".</summary>
    public void Mostrar()
    {
        gameObject.SetActive(true);
        StartCoroutine(FadeEntradaCO());
    }

    // ─────────────────────────────────────────────────────────────────────
    public void Regresar()
    {
        StartCoroutine(RegresarCO());
    }

    IEnumerator RegresarCO()
    {
        // NOTA: el botón "Regresar" ya NO se desactiva en ningún momento,
        // se mantiene siempre interactuable.
        yield return Fade(0f, 1f, duracionFade);

        // Ocultar este panel y mostrar el menú
        gameObject.SetActive(false);

        if (menuInicio != null)
            menuInicio.MostrarMenuDesdeCreditos();

        yield return Fade(1f, 0f, duracionFade);
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator FadeEntradaCO()
    {
        SetAlpha(1f);
        yield return Fade(1f, 0f, duracionFade);
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

