using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Menú principal de la experiencia.
///
/// CAMBIO RESPECTO A LA VERSIÓN ANTERIOR:
///   El campo "escenaPrincipal" ahora se llama "escenaCinematica".
///   En el Inspector asigna "Cinematica" (o el nombre exacto de tu escena de intro).
///   El flujo queda: MenuInicio → Cinematica → EscenaPrincipal
///
/// SETUP EN UNITY:
///   Canvas
///     ├── Fondo          ← Image que cubre toda la pantalla
///     ├── Titulo         ← Text con el nombre de la experiencia
///     ├── Subtitulo      ← Text secundario (opcional)
///     ├── BotonIniciar   ← Button → OnClick → MenuInicio.IniciarExperiencia()
///     ├── BotonSalir     ← Button → OnClick → MenuInicio.Salir()
///     └── PanelFade      ← Image negra stretch completo, alpha 0 al inicio
/// </summary>
public class MenuInicio : MonoBehaviour
{
    [Header("── Escenas ──────────────────────────────")]
    [Tooltip("Nombre exacto de tu escena de cinemática de intro")]
    public string escenaCinematica = "Cinematica";      // ← antes era escenaPrincipal

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

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (textoTitulo != null) textoTitulo.text = titulo;
        if (textoSubtitulo != null) textoSubtitulo.text = subtitulo;

        if (botonIniciar != null) { botonIniciar.onClick.RemoveAllListeners(); botonIniciar.onClick.AddListener(IniciarExperiencia); }
        if (botonSalir != null) { botonSalir.onClick.RemoveAllListeners(); botonSalir.onClick.AddListener(Salir); }

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

        // Resetear GameManager si viene de un reinicio
        if (GameManager.Instance != null)
            GameManager.Instance.Reiniciar();

        SceneManager.LoadScene(escenaCinematica);   // ← va a Cinematica, no al juego directo
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
        Application.Quit();
        Debug.Log("[MenuInicio] Salir — funciona en build, no en el editor.");
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
