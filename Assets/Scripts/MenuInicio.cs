using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Menú principal de la experiencia.
///
/// SETUP EN UNITY:
///   1. Crea una nueva escena "MenuInicio"
///   2. Agrégala en Build Settings como la primera escena (índice 0)
///   3. Crea un Canvas (Screen Space - Overlay) con esta estructura:
///
///   Canvas
///     ├── Fondo          ← Image que cubre toda la pantalla
///     ├── Titulo         ← Text con el nombre de la experiencia
///     ├── Subtitulo      ← Text secundario (opcional)
///     ├── BotonIniciar   ← Button "Iniciar experiencia"
///     └── BotonSalir     ← Button "Salir"
///
///   4. Crea un GameObject vacío → Add Component → MenuInicio
///   5. Asigna los campos en el Inspector
///   6. En cada botón → OnClick → MenuInicio → IniciarExperiencia() / Salir()
/// </summary>
public class MenuInicio : MonoBehaviour
{
    [Header("── Escena a cargar ──────────────────────")]
    public string escenaPrincipal = "EscenaPrincipal";  // nombre exacto de tu escena

    [Header("── UI ──────────────────────────────────")]
    public Text   textoTitulo;
    public Text   textoSubtitulo;
    public Button botonIniciar;
    public Button botonSalir;

    [Header("── Panel negro para fade ───────────────")]
    public Image panelFade;   // Image negra en stretch completo, alpha 0 al inicio

    [Header("── Contenido ───────────────────────────")]
    public string titulo    = "Experiencia Interactiva";
    public string subtitulo = "Una historia sobre grooming";

    [Header("── Tiempos ─────────────────────────────")]
    public float duracionFadeIn  = 0.8f;   // fade al abrir el menú
    public float duracionFadeOut = 1.0f;   // fade al iniciar la experiencia

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        // Asignar textos
        if (textoTitulo    != null) textoTitulo.text    = titulo;
        if (textoSubtitulo != null) textoSubtitulo.text = subtitulo;

        // Asignar OnClick dinámicamente
        if (botonIniciar != null) { botonIniciar.onClick.RemoveAllListeners(); botonIniciar.onClick.AddListener(IniciarExperiencia); }
        if (botonSalir   != null) { botonSalir.onClick.RemoveAllListeners();   botonSalir.onClick.AddListener(Salir); }

        // Fade de entrada
        StartCoroutine(FadeEntradaCO());
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator FadeEntradaCO()
    {
        if (panelFade == null) yield break;

        // Empezar negro y aclarar
        SetAlpha(1f);
        yield return Fade(1f, 0f, duracionFadeIn);
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>Asigna al BotonIniciar en OnClick o se asigna automáticamente en Start.</summary>
    public void IniciarExperiencia()
    {
        StartCoroutine(IniciarCO());
    }

    IEnumerator IniciarCO()
    {
        // Desactivar botones para evitar doble clic
        if (botonIniciar != null) botonIniciar.interactable = false;
        if (botonSalir   != null) botonSalir.interactable   = false;

        // Fade a negro
        yield return Fade(0f, 1f, duracionFadeOut);

        // Resetear GameManager si existe (por si viene de un reinicio)
        if (GameManager.Instance != null)
            GameManager.Instance.Reiniciar();

        SceneManager.LoadScene(escenaPrincipal);
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>Asigna al BotonSalir en OnClick o se asigna automáticamente en Start.</summary>
    public void Salir()
    {
        StartCoroutine(SalirCO());
    }

    IEnumerator SalirCO()
    {
        if (botonIniciar != null) botonIniciar.interactable = false;
        if (botonSalir   != null) botonSalir.interactable   = false;

        yield return Fade(0f, 1f, duracionFadeOut);

        Application.Quit();

        // En el editor no cierra, solo muestra mensaje
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
