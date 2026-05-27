using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel de misión/objetivo en la esquina superior derecha.
/// Aparece con fade al iniciar un puzzle y desaparece al completarlo.
/// Todos los puzzles (RecolectorMonedas, PuzzlePalancas) lo llaman.
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
///
/// USO DESDE OTROS SCRIPTS:
///   UIObjetivo.Instance.MostrarObjetivo("Recoge las monedas", 0, 3);
///   UIObjetivo.Instance.ActualizarProgreso(1, 3);
///   UIObjetivo.Instance.CompletarObjetivo();      // fade out automático
/// ══════════════════════════════════════════════════════
/// </summary>
public class UIObjetivo : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────
    public static UIObjetivo Instance { get; private set; }

    [Header("── UI ──────────────────────────────────")]
    public GameObject  panelObjetivo;
    public CanvasGroup canvasGroup;
    public Text        textoMision;
    public Text        textoProgreso;

    [Header("── Tiempos ─────────────────────────────")]
    public float duracionFade     = 0.4f;
    public float delayAlCompletar = 1.5f;   // segundos visible tras completar

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (panelObjetivo != null) panelObjetivo.SetActive(false);
        if (canvasGroup   != null) canvasGroup.alpha = 0f;
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Muestra el panel con fade in.
    /// Llamar al inicio de cada puzzle.
    /// </summary>
    public void MostrarObjetivo(string descripcion, int actual, int total)
    {
        StopAllCoroutines();

        if (textoMision   != null) textoMision.text   = descripcion;
        if (textoProgreso != null) textoProgreso.text = FormatearProgreso(actual, total);

        if (panelObjetivo != null) panelObjetivo.SetActive(true);
        StartCoroutine(FadeCO(0f, 1f, duracionFade));
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Actualiza solo el contador sin rehacer el fade.
    /// Llamar cada vez que se recoge una moneda o se activa una palanca.
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
    IEnumerator CompletarCO()
    {
        // Asegurar que está visible
        yield return FadeCO(canvasGroup != null ? canvasGroup.alpha : 1f, 1f, 0.1f);
        yield return new WaitForSeconds(delayAlCompletar);
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
    string FormatearProgreso(int actual, int total)
        => $"{actual} / {total}";
}
