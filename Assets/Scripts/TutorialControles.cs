using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Muestra el panel de controles al inicio del Día 1 durante unos segundos.
/// El jugador puede moverse libremente mientras el panel está visible.
/// Desaparece solo (timer) o al presionar Tab/cualquier tecla configurable.
///
/// ══════════════════════════════════════════════════════════════════
/// JERARQUÍA EN UNITY
/// ══════════════════════════════════════════════════════════════════
///
///  Canvas (Sort Order 10 — por encima del HUD pero debajo del fade)
///    └── PanelTutorial
///          Anchor: bottom-left   Pivot: (0, 0)
///          Pos: (40, 40)         Size: (340, 290)
///          Image: color #0D0D0D  Alpha: 210
///          CanvasGroup: Alpha 1, Interactable OFF, BlockRaycasts OFF
///          └── VerticalLayoutGroup:
///                Padding: Left 20, Right 20, Top 18, Bottom 18
///                Spacing: 0
///                ChildForceExpandWidth: true
///                ChildForceExpandHeight: false
///                ChildAlignment: UpperLeft
///
///          ├── TextoTitulo          ← Text "CONTROLES"
///          │     Font size: 15   Bold   Color: #FFFFFF
///          │     LayoutElement: MinHeight 28
///          │
///          ├── Separador            ← Image   Color: #FFFFFF  Alpha: 60
///          │     LayoutElement: MinHeight 1   PreferredHeight 1
///          │                    MinWidth 0    FlexibleWidth 1
///          │
///          ├── FilaMovimiento       ← fila de control (ver estructura Fila)
///          ├── FilaInteractuar
///          ├── FilaDialogo
///          ├── FilaMirar
///          ├── FilaCorrer
///          │
///          └── TextoCerrar          ← Text "[TAB] Cerrar"
///                Font size: 12   Color: #888888   Italic
///                LayoutElement: MinHeight 22
///                Alignment: LowerRight
///
///  ── Estructura de cada Fila ──────────────────────────────────────
///  FilaXxx  (GameObject vacío)
///    HorizontalLayoutGroup:
///      Spacing: 8
///      ChildForceExpandWidth: false  ChildForceExpandHeight: false
///      ChildAlignment: MiddleLeft
///    LayoutElement: MinHeight 36
///
///    ├── TextoAccion   ← Text  "MOVIMIENTO"
///    │     Font size: 12   Bold   Color: #AAAAAA
///    │     LayoutElement: MinWidth 160   FlexibleWidth 0
///    │
///    └── TextoTecla    ← Text  "WASD"
///          Font size: 12   Color: #FFFFFF
///          LayoutElement: FlexibleWidth 1
///
/// ══════════════════════════════════════════════════════════════════
/// INSPECTOR DEL SCRIPT
/// ══════════════════════════════════════════════════════════════════
///   panelTutorial   → PanelTutorial
///   tiempoVisible   → 15
///   teclaOcultar    → Tab  (o la que prefieras)
///   duracionFade    → 0.6
/// ══════════════════════════════════════════════════════════════════
/// </summary>
public class TutorialControles : MonoBehaviour
{
    [Header("── UI ──────────────────────────────────")]
    public CanvasGroup panelTutorial;

    [Header("── Tiempos ─────────────────────────────")]
    [Tooltip("Segundos que permanece visible antes de desaparecer solo")]
    public float tiempoVisible = 15f;
    public float duracionFade  = 0.6f;

    [Header("── Tecla para cerrar manualmente ───────")]
    public Key teclaOcultar = Key.Tab;

    [Header("── Solo mostrar en el Día 1 ─────────────")]
    [Tooltip("Si está activo, no aparece si se reinicia desde un día mayor (debug)")]
    public bool soloEnDia1 = true;

    bool _visible = false;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (panelTutorial == null) return;

        // No mostrar si se está haciendo debug desde un momento avanzado
        if (soloEnDia1 && GameManager.Instance != null && GameManager.Instance.DiaActual > 1)
        {
            panelTutorial.alpha          = 0f;
            panelTutorial.interactable   = false;
            panelTutorial.blocksRaycasts = false;
            return;
        }

        panelTutorial.alpha          = 0f;
        panelTutorial.interactable   = false;
        panelTutorial.blocksRaycasts = false;

        // Espera a que TransicionDia termine su fade de Día 1 antes de aparecer
        // (TransicionDia usa duracionFadeOut ~1s + duracionTexto ~2s → ~3s total)
        StartCoroutine(MostrarConDelayyCO());
    }

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (!_visible) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current[teclaOcultar].wasPressedThisFrame)
            StartCoroutine(OcultarCO());
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator MostrarConDelayyCO()
    {
        // Espera a que el fade de Día 1 de TransicionDia haya terminado
        // TransicionDia: duracionTexto(2) + duracionFadeOut(1) = ~3.5s
        // Añadimos un pequeño margen extra para que el jugador ya tenga control
        yield return new WaitForSeconds(3.8f);

        yield return FadePanel(0f, 1f, duracionFade);
        _visible = true;

        yield return new WaitForSeconds(tiempoVisible);

        if (_visible)
            StartCoroutine(OcultarCO());
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator OcultarCO()
    {
        _visible = false;
        yield return FadePanel(1f, 0f, duracionFade);
        panelTutorial.blocksRaycasts = false;
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator FadePanel(float desde, float hasta, float duracion)
    {
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            panelTutorial.alpha = Mathf.Lerp(desde, hasta, t / duracion);
            yield return null;
        }
        panelTutorial.alpha = hasta;
    }
}
