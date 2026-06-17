using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel de misión/objetivo en la esquina superior derecha.
/// Aparece con fade al iniciar un puzzle y desaparece al completarlo.
/// Todos los puzzles (RecolectorMonedas, GestorZonas) lo llaman.
///
/// SETUP EN UNITY — jerarquía:
/// ══════════════════════════════════════════════════════
/// Canvas  (Screen Space - Overlay, Sort Order 20)
///   └── PanelObjetivo                    ← este panel, SetActive(false) al inicio
///         │  Anchor: top-right
///         │  Pivot: (1, 1)
///         │  Pos: (-20, -20)             ← margen desde la esquina
///         │  Size: (280, 100)
///         │  Image: fondo oscuro semitransparente (0,0,0, alpha 180)
///         │  CanvasGroup: Alpha 0 al inicio  ← para el fade
///         │
///         ├── TextoTitulo               ← Text  "OBJETIVO"
///         │     Font size: 11  Bold  Color: #AAAAAA  mayúsculas
///         │     Anchor: top-left  Pos: (12, -10)
///         │
///         ├── TextoMision               ← Text  (descripción dinámica)
///         │     Font size: 14  Bold  Color: blanco
///         │     Anchor: top-left  Pos: (12, -30)
///         │     Width: 256
///         │
///         └── TextoProgreso             ← Text  "0 / 3"
///               Font size: 13  Color: #CCCCCC
///               Anchor: top-left  Pos: (12, -52)
///
/// INSPECTOR — campos del script UIObjetivo:
///   panelObjetivo    → PanelObjetivo
///   canvasGroup      → CanvasGroup del PanelObjetivo
///   textoMision      → TextoMision
///   textoProgreso    → TextoProgreso
///   duracionFade     → 0.4
///   delayAlCompletar → 1.5   (segundos visible tras completar antes de desaparecer)
///   sonidoMostrar    → clip que suena al aparecer el panel
///   sonidoCompletar  → clip que suena al completar / mostrar siguiente paso
///
/// USO DESDE OTROS SCRIPTS:
///   UIObjetivo.Instance.MostrarObjetivo("Recoge las monedas", 0, 3);
///   UIObjetivo.Instance.ActualizarProgreso(1, 3);
///   UIObjetivo.Instance.CompletarObjetivo();          // fade out automático
///   UIObjetivo.Instance.MostrarSiguientePaso("Vuelve a hablar con el NPC");
/// ══════════════════════════════════════════════════════
/// </summary>
public class UIObjetivo : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────
    public static UIObjetivo Instance { get; private set; }

    [Header("── UI ──────────────────────────────────")]
    public GameObject panelObjetivo;
    public CanvasGroup canvasGroup;
    public Text textoMision;
    public Text textoProgreso;

    [Header("── Tiempos ─────────────────────────────")]
    public float duracionFade = 0.4f;
    public float delayAlCompletar = 1.5f;   // segundos visible tras completar

    [Header("── Audio ───────────────────────────────")]
    [Tooltip("Sonido al aparecer el panel de objetivo")]
    public AudioClip sonidoMostrar;
    [Tooltip("Sonido al completar el objetivo o mostrar siguiente paso")]
    public AudioClip sonidoCompletar;
    [Range(0f, 1f)]
    public float volumenUI = 0.9f;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (panelObjetivo != null) panelObjetivo.SetActive(false);
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Muestra el panel con fade in.
    /// Llamar al inicio de cada puzzle.
    /// </summary>
    public void MostrarObjetivo(string descripcion, int actual, int total)
    {
        StopAllCoroutines();

        if (textoMision != null) textoMision.text = descripcion;
        if (textoProgreso != null) textoProgreso.text = FormatearProgreso(actual, total);

        if (panelObjetivo != null) panelObjetivo.SetActive(true);
        StartCoroutine(FadeCO(0f, 1f, duracionFade));

        // ── Audio al mostrar ──────────────────────────────────────────────
        ReproducirSonido(sonidoMostrar);
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Actualiza solo el contador sin rehacer el fade.
    /// Llamar cada vez que se recoge una moneda o se activa una zona.
    /// </summary>
    public void ActualizarProgreso(int actual, int total)
    {
        if (textoProgreso != null)
            textoProgreso.text = FormatearProgreso(actual, total);
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Marca como completado, espera delayAlCompletar y hace fade out.
    /// </summary>
    public void CompletarObjetivo()
    {
        StopAllCoroutines();
        if (textoProgreso != null) textoProgreso.text = "¡Completado!";
        StartCoroutine(CompletarCO());
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Muestra el mensaje de siguiente paso (ej. "Vuelve a hablar con el NPC")
    /// reemplazando el texto de misión, sin mostrar progreso.
    /// Luego desaparece automáticamente.
    /// </summary>
    public void MostrarSiguientePaso(string mensaje)
    {
        StopAllCoroutines();

        if (textoMision != null) textoMision.text = mensaje;
        if (textoProgreso != null) textoProgreso.text = "";

        if (panelObjetivo != null) panelObjetivo.SetActive(true);

        // ── Audio al completar/siguiente paso ─────────────────────────────
        ReproducirSonido(sonidoCompletar);

        StartCoroutine(SiguientePasoCO());
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator CompletarCO()
    {
        // ── Audio al completar ────────────────────────────────────────────
        ReproducirSonido(sonidoCompletar);

        // Asegurar que está visible
        yield return FadeCO(canvasGroup != null ? canvasGroup.alpha : 1f, 1f, 0.1f);
        yield return new WaitForSeconds(delayAlCompletar);
        yield return FadeCO(1f, 0f, duracionFade);
        if (panelObjetivo != null) panelObjetivo.SetActive(false);
    }

    IEnumerator SiguientePasoCO()
    {
        // Fade in desde donde esté
        float alphaInicial = canvasGroup != null ? canvasGroup.alpha : 0f;
        yield return FadeCO(alphaInicial, 1f, duracionFade);

        // Espera un poco más para que sea legible
        yield return new WaitForSeconds(delayAlCompletar + 1f);

        yield return FadeCO(1f, 0f, duracionFade);
        if (panelObjetivo != null) panelObjetivo.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator FadeCO(float desde, float hasta, float duracion)
    {
        if (canvasGroup == null) yield break;
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(desde, hasta, t / duracion);
            yield return null;
        }
        canvasGroup.alpha = hasta;
    }

    // ─────────────────────────────────────────────────────────────────────
    void ReproducirSonido(AudioClip clip)
    {
        if (clip == null) return;
        if (Camera.main != null)
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, volumenUI);
    }

    // ─────────────────────────────────────────────────────────────────────
    string FormatearProgreso(int actual, int total)
        => $"{actual} / {total}";
}
