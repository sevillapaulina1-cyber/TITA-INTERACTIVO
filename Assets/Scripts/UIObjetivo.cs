using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel de misión/objetivo en la esquina superior derecha.
/// Al completar un puzzle muestra un panel CENTRAL con imagen durante
/// duracionCompletado segundos, luego ese panel desaparece y aparece el
/// objetivo persistente "Vuelve a hablar con SamuVR" en la esquina.
///
/// SETUP EN UNITY — jerarquía:
/// ══════════════════════════════════════════════════════
/// Canvas  (Screen Space - Overlay, Sort Order 20)
///   ├── PanelObjetivo                    ← panel esquina superior derecha
///   │     │  Anchor: top-right  Pivot: (1,1)  Pos: (-20,-20)  Size: (280,100)
///   │     │  Image fondo oscuro  +  CanvasGroup alpha 0
///   │     ├── TextoTitulo               ← "OBJETIVO"
///   │     ├── TextoMision               ← descripción dinámica
///   │     └── TextoProgreso             ← "0 / 3"
///   │
///   └── PanelCompletado                  ← panel central de completado
///         │  Anchor: center  Pivot: (0.5,0.5)  Pos: (0,0)  Size: (400,220)
///         │  SetActive(false) al inicio  +  CanvasGroup alpha 0
///         ├── ImagenCompletado           ← Image con tu sprite de completado
///         └── TextoCompletadoLabel       ← Text "¡Completado!" (opcional)
///
/// INSPECTOR:
///   panelObjetivo         → PanelObjetivo
///   canvasGroup           → CanvasGroup de PanelObjetivo
///   textoMision / textoProgreso
///   panelCompletado       → PanelCompletado
///   canvasGroupCompletado → CanvasGroup de PanelCompletado
///   imagenCompletado      → Image del panel central
///   textoCompletadoLabel  → Text opcional debajo de la imagen
///   duracionCompletado    → 4
///   sonidoMostrar / sonidoCompletar
///
/// USO PRINCIPAL DESDE RecolectorMonedas y GestorZonas:
///   UIObjetivo.Instance.MostrarPantallaCompletado("Vuelve a hablar con SamuVR");
/// ══════════════════════════════════════════════════════
/// </summary>
public class UIObjetivo : MonoBehaviour
{
    public static UIObjetivo Instance { get; private set; }

    [Header("── Panel esquina (objetivo) ─────────────")]
    public GameObject panelObjetivo;
    public CanvasGroup canvasGroup;
    public Text textoMision;
    public Text textoProgreso;

    [Header("── Panel central (completado) ───────────")]
    [Tooltip("Panel que aparece al centro al completar el puzzle")]
    public GameObject panelCompletado;
    [Tooltip("CanvasGroup del PanelCompletado para el fade")]
    public CanvasGroup canvasGroupCompletado;
    [Tooltip("Image del panel central — asigna tu sprite de completado")]
    public Image imagenCompletado;
    [Tooltip("Texto opcional debajo de la imagen")]
    public Text textoCompletadoLabel;

    [Header("── Tiempos ─────────────────────────────")]
    public float duracionFade = 0.4f;
    public float delayAlCompletar = 1.5f;   // conservado por compatibilidad
    [Tooltip("Segundos que se muestra el panel central de completado")]
    public float duracionCompletado = 4f;

    [Header("── Audio ───────────────────────────────")]
    [Tooltip("Sonido al aparecer el panel de objetivo (esquina)")]
    public AudioClip sonidoMostrar;
    [Tooltip("Sonido al aparecer el panel central de completado")]
    public AudioClip sonidoCompletar;
    [Range(0f, 1f)]
    public float volumenUI = 0.9f;

    AudioSource _audioSource;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (panelObjetivo != null) panelObjetivo.SetActive(false);
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (panelCompletado != null) panelCompletado.SetActive(false);
        if (canvasGroupCompletado != null) canvasGroupCompletado.alpha = 0f;

        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        _audioSource.spatialBlend = 0f;
        _audioSource.volume = volumenUI;
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>Muestra el panel esquina al inicio de un puzzle.</summary>
    public void MostrarObjetivo(string descripcion, int actual, int total)
    {
        StopAllCoroutines();

        if (textoMision != null) textoMision.text = descripcion;
        if (textoProgreso != null) textoProgreso.text = FormatearProgreso(actual, total);

        if (panelObjetivo != null) panelObjetivo.SetActive(true);
        StartCoroutine(FadeEsquinaCO(0f, 1f, duracionFade));
        ReproducirSonido(sonidoMostrar);
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>Actualiza solo el contador sin rehacer el fade.</summary>
    public void ActualizarProgreso(int actual, int total)
    {
        if (textoProgreso != null)
            textoProgreso.text = FormatearProgreso(actual, total);
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Flujo completo al completar un puzzle:
    ///   1. Oculta el panel esquina
    ///   2. Muestra el panel central con imagen + sonido durante duracionCompletado seg
    ///   3. Oculta el panel central
    ///   4. Muestra el objetivo persistente mensajeSiguiente en la esquina
    /// Llamar desde RecolectorMonedas y GestorZonas.
    /// </summary>
    public void MostrarPantallaCompletado(string mensajeSiguiente)
    {
        StopAllCoroutines();
        StartCoroutine(FlujoPantallaCompletadoCO(mensajeSiguiente));
    }

    IEnumerator FlujoPantallaCompletadoCO(string mensajeSiguiente)
    {
        // 1. Ocultar panel esquina
        float alphaEsquina = canvasGroup != null ? canvasGroup.alpha : 1f;
        yield return FadeEsquinaCO(alphaEsquina, 0f, duracionFade * 0.5f);
        if (panelObjetivo != null) panelObjetivo.SetActive(false);

        // 2. Mostrar panel central
        if (panelCompletado != null) panelCompletado.SetActive(true);
        ReproducirSonido(sonidoCompletar);
        yield return FadeCentralCO(0f, 1f, duracionFade);

        // 3. Esperar
        yield return new WaitForSeconds(duracionCompletado);

        // 4. Ocultar panel central
        yield return FadeCentralCO(1f, 0f, duracionFade);
        if (panelCompletado != null) panelCompletado.SetActive(false);

        // 5. Mostrar objetivo persistente en esquina
        MostrarObjetivoPersistente(mensajeSiguiente);
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Compatibilidad con código existente que llama CompletarObjetivo().
    /// Muestra el panel central sin siguiente paso.
    /// </summary>
    public void CompletarObjetivo()
    {
        StopAllCoroutines();
        StartCoroutine(CompletarSimpleCO());
    }

    IEnumerator CompletarSimpleCO()
    {
        float alphaEsquina = canvasGroup != null ? canvasGroup.alpha : 1f;
        yield return FadeEsquinaCO(alphaEsquina, 0f, duracionFade * 0.5f);
        if (panelObjetivo != null) panelObjetivo.SetActive(false);

        if (panelCompletado != null) panelCompletado.SetActive(true);
        ReproducirSonido(sonidoCompletar);
        yield return FadeCentralCO(0f, 1f, duracionFade);
        yield return new WaitForSeconds(duracionCompletado);
        yield return FadeCentralCO(1f, 0f, duracionFade);
        if (panelCompletado != null) panelCompletado.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Objetivo persistente en esquina — NO desaparece solo.
    /// Cerrar con OcultarObjetivo() cuando el jugador presione E.
    /// </summary>
    public void MostrarObjetivoPersistente(string mensaje)
    {
        StopAllCoroutines();

        if (textoMision != null) textoMision.text = mensaje;
        if (textoProgreso != null) textoProgreso.text = "";

        if (panelObjetivo != null) panelObjetivo.SetActive(true);
        ReproducirSonido(sonidoMostrar);
        StartCoroutine(FadeEsquinaCO(canvasGroup != null ? canvasGroup.alpha : 0f, 1f, duracionFade));
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>Cierra el panel de esquina con fade. Llamar al presionar E.</summary>
    public void OcultarObjetivo()
    {
        StopAllCoroutines();
        StartCoroutine(OcultarCO());
    }

    IEnumerator OcultarCO()
    {
        yield return FadeEsquinaCO(canvasGroup != null ? canvasGroup.alpha : 1f, 0f, duracionFade);
        if (panelObjetivo != null) panelObjetivo.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>Autooculta con temporizador (usos simples).</summary>
    public void MostrarSiguientePaso(string mensaje)
    {
        StopAllCoroutines();

        if (textoMision != null) textoMision.text = mensaje;
        if (textoProgreso != null) textoProgreso.text = "";

        if (panelObjetivo != null) panelObjetivo.SetActive(true);
        ReproducirSonido(sonidoCompletar);
        StartCoroutine(SiguientePasoCO());
    }

    IEnumerator SiguientePasoCO()
    {
        float alphaInicial = canvasGroup != null ? canvasGroup.alpha : 0f;
        yield return FadeEsquinaCO(alphaInicial, 1f, duracionFade);
        yield return new WaitForSeconds(delayAlCompletar + 1f);
        yield return FadeEsquinaCO(1f, 0f, duracionFade);
        if (panelObjetivo != null) panelObjetivo.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator FadeEsquinaCO(float desde, float hasta, float duracion)
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

    IEnumerator FadeCentralCO(float desde, float hasta, float duracion)
    {
        if (canvasGroupCompletado == null) yield break;
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            canvasGroupCompletado.alpha = Mathf.Lerp(desde, hasta, t / duracion);
            yield return null;
        }
        canvasGroupCompletado.alpha = hasta;
    }

    // ─────────────────────────────────────────────────────────────────────
    void ReproducirSonido(AudioClip clip)
    {
        if (clip == null || _audioSource == null) return;
        _audioSource.volume = volumenUI;
        _audioSource.PlayOneShot(clip);
    }

    string FormatearProgreso(int actual, int total) => $"{actual} / {total}";
}
