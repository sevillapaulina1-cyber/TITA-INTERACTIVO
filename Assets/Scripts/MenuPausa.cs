using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

/// <summary>
/// Menú de pausa — diseño de una sola pantalla.
/// Controles visibles siempre a la izquierda, botones a la derecha.
/// El panel de Audio reemplaza los botones en el lado derecho cuando se abre.
///
/// FLUJO:
///   ESC → aparece el menú (Time.timeScale = 0)
///   ESC o "Resume" → cierra y reanuda
///   "Options" → PanelBotones se oculta, PanelAudio aparece en su lugar
///   "← Volver" → regresa a PanelBotones
///   "Quit" → fade a negro → MenuInicio
///
/// ══════════════════════════════════════════════════════════════════
/// JERARQUÍA COMPLETA EN UNITY
/// ══════════════════════════════════════════════════════════════════
///
/// Canvas  (Screen Space - Overlay, Sort Order 50)
///   │
///   ├── PanelPausa                         ← SetActive(false) al inicio
///   │     Anchor: stretch completo
///   │     Image color: #000000  Alpha: 200 (negro semitransparente)
///   │
///   │     ├── TextoTitulo                  ← Text "PAUSED"
///   │     │     Anchor: top-center
///   │     │     Pos Y: -60   Font size: 52   Bold   Color: #D93030
///   │     │
///   │     ├── LineaHorizontal              ← Image
///   │     │     Anchor: top-stretch
///   │     │     Pos Y: -110   Height: 2   Color: blanco Alpha 80
///   │     │
///   │     ├── PanelControles               ← Panel izquierdo (sin Image)
///   │     │     Anchor: middle-left   Pivot: (0, 0.5)
///   │     │     Pos: (80, -20)   Size: (460, 380)
///   │     │     ── VerticalLayoutGroup ──
///   │     │        Spacing: 0
///   │     │        ChildAlignment: UpperLeft
///   │     │        ChildForceExpandWidth: false
///   │     │        ChildForceExpandHeight: false
///   │     │
///   │     │     ├── TextoSeccion           ← Text "Movement"
///   │     │     │     Font size: 16   Bold   Color: blanco   Height: 30
///   │     │     │     LayoutElement: MinHeight 30, PreferredHeight 30
///   │     │     │     Padding bottom en el VerticalLayoutGroup: 12
///   │     │     │
///   │     │     │  [Para cada fila de control, crea un hijo con esta estructura:]
///   │     │     │
///   │     │     ├── FilaMovimiento         ← GameObject vacío   Height: 36
///   │     │     │     ── HorizontalLayoutGroup ──
///   │     │     │        ChildAlignment: MiddleLeft
///   │     │     │        ChildForceExpandHeight: false
///   │     │     │     ├── TextoAccion      ← Text "Movimiento"
///   │     │     │     │     Width: 220   Font size: 15   Color: #AAAAAA
///   │     │     │     │     LayoutElement: MinWidth 220, PreferredWidth 220
///   │     │     │     └── TextoTecla       ← Text "WASD"
///   │     │     │           Width: 160   Font size: 15   Color: blanco
///   │     │     │
///   │     │     ├── FilaInteractuar        Height: 36
///   │     │     │     TextoAccion "Interactuar"  /  TextoTecla "E"
///   │     │     │
///   │     │     ├── FilaDialogo            Height: 36
///   │     │     │     TextoAccion "Avanzar diálogo"  /  TextoTecla "Clic izquierdo"
///   │     │     │
///   │     │     ├── FilaMirar              Height: 36
///   │     │     │     TextoAccion "Mirar"  /  TextoTecla "Ratón"
///   │     │     │
///   │     │     └── FilaPausa              Height: 36
///   │     │           TextoAccion "Pausa"  /  TextoTecla "ESC"
///   │     │
///   │     ├── LineaVertical               ← Image
///   │     │     Anchor: middle-center
///   │     │     Pos: (0, -20)   Size: (2, 360)   Color: blanco Alpha 50
///   │     │
///   │     ├── PanelBotones                ← Panel derecho (sin Image)
///   │     │     Anchor: middle-right   Pivot: (1, 0.5)
///   │     │     Pos: (-80, -20)   Size: (280, 360)
///   │     │     ── VerticalLayoutGroup ──
///   │     │        Spacing: 28
///   │     │        ChildAlignment: MiddleCenter
///   │     │        ChildForceExpandWidth: true
///   │     │        ChildForceExpandHeight: false
///   │     │
///   │     │     ├── BotonContinuar         ← Button   Height: 54
///   │     │     │     Image: transparente (Alpha 0) o sin Image
///   │     │     │     └── Text "Resume"
///   │     │     │           Font size: 24   Color: blanco
///   │     │     │           Alignment: MiddleCenter
///   │     │     │
///   │     │     ├── BotonAudio             ← Button   Height: 54
///   │     │     │     └── Text "Options"
///   │     │     │           Font size: 24   Color: blanco
///   │     │     │
///   │     │     └── BotonSalir             ← Button   Height: 54
///   │     │           └── Text "Quit"
///   │     │                 Font size: 24   Color: blanco
///   │     │
///   │     └── PanelAudio                  ← misma pos que PanelBotones
///   │           SetActive(false) al inicio
///   │           Anchor: middle-right   Pivot: (1, 0.5)
///   │           Pos: (-80, -20)   Size: (280, 360)
///   │           ── VerticalLayoutGroup ──
///   │              Spacing: 16
///   │              ChildAlignment: UpperCenter
///   │              ChildForceExpandWidth: true
///   │
///   │           ├── TextoAudioTitulo       ← Text "Options"   Font size: 22   Bold
///   │           │     Height: 40
///   │           │
///   │           ├── TextoMusica            ← Text "Música"   Font size: 14   Color: #AAAAAA
///   │           │     Height: 24
///   │           │
///   │           ├── SliderMusica           ← Slider   Height: 24
///   │           │     Min: 0   Max: 1   Value: 1
///   │           │     OnValueChanged → MenuPausa.CambiarVolumenMusica()
///   │           │
///   │           ├── TextoSFX               ← Text "Efectos de sonido"   Height: 24
///   │           │
///   │           ├── SliderSFX              ← Slider   Height: 24
///   │           │     OnValueChanged → MenuPausa.CambiarVolumenSFX()
///   │           │
///   │           └── BotonVolver            ← Button   Height: 44
///   │                 └── Text "← Volver"   Font size: 18   Color: blanco
///   │
///   └── PanelFade                         ← Sort Order 99
///         Image negra stretch completo   Alpha: 0 al inicio
///
/// ══════════════════════════════════════════════════════════════════
/// INSPECTOR — campos del script MenuPausa
/// ══════════════════════════════════════════════════════════════════
///
///   Escenas
///     escenaMenu             → "MenuInicio"
///
///   Panels
///     panelPausa             → PanelPausa
///     panelBotones           → PanelBotones
///     panelAudio             → PanelAudio
///
///   Fade
///     panelFade              → PanelFade (Image Sort Order 99)
///     duracionFade           → 0.8
///
///   Audio Mixer
///     audioMixer             → MixerPrincipal
///     parametroMusica        → "VolMusica"
///     parametroSFX           → "VolSFX"
///     sliderMusica           → SliderMusica
///     sliderSFX              → SliderSFX
///
///   Jugador
///     firstPersonController  → tu script de movimiento FPS
///
/// ══════════════════════════════════════════════════════════════════
/// BOTONES — OnClick en el Inspector
/// ══════════════════════════════════════════════════════════════════
///   BotonContinuar  → MenuPausa.Continuar()
///   BotonAudio      → MenuPausa.AbrirAudio()
///   BotonSalir      → MenuPausa.Salir()
///   BotonVolver     → MenuPausa.VolverBotones()
/// ══════════════════════════════════════════════════════════════════
/// </summary>
public class MenuPausa : MonoBehaviour
{
    [Header("── Escenas ──────────────────────────────")]
    public string escenaMenu = "MenuInicio";

    [Header("── Panels ───────────────────────────────")]
    public GameObject panelPausa;
    public GameObject panelBotones;
    public GameObject panelAudio;

    [Header("── Fade ─────────────────────────────────")]
    public Image panelFade;
    public float duracionFade = 0.8f;

    [Header("── Audio Mixer ─────────────────────────")]
    public AudioMixer audioMixer;
    public string     parametroMusica = "VolMusica";
    public string     parametroSFX   = "VolSFX";
    public Slider     sliderMusica;
    public Slider     sliderSFX;

    [Header("── Jugador ──────────────────────────────")]
    public MonoBehaviour firstPersonController;

    // ── Estado ────────────────────────────────────────────────────────────
    bool _pausado = false;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (panelPausa  != null) panelPausa.SetActive(false);
        if (panelAudio  != null) panelAudio.SetActive(false);
        if (panelFade   != null) SetAlpha(0f);

        InicializarSlider(sliderMusica, parametroMusica, CambiarVolumenMusica);
        InicializarSlider(sliderSFX,    parametroSFX,   CambiarVolumenSFX);
    }

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (_pausado) Continuar();
            else          Pausar();
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    void Pausar()
    {
        _pausado       = true;
        Time.timeScale = 0f;

        if (firstPersonController != null)
            firstPersonController.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        if (panelPausa   != null) panelPausa.SetActive(true);
        if (panelBotones != null) panelBotones.SetActive(true);
        if (panelAudio   != null) panelAudio.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>Botón "Resume" y ESC cuando está pausado.</summary>
    public void Continuar()
    {
        _pausado       = false;
        Time.timeScale = 1f;

        if (panelPausa != null) panelPausa.SetActive(false);

        if (firstPersonController != null)
            firstPersonController.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>Botón "Options".</summary>
    public void AbrirAudio()
    {
        if (panelBotones != null) panelBotones.SetActive(false);
        if (panelAudio   != null) panelAudio.SetActive(true);
    }

    /// <summary>Botón "← Volver" en PanelAudio.</summary>
    public void VolverBotones()
    {
        if (panelAudio   != null) panelAudio.SetActive(false);
        if (panelBotones != null) panelBotones.SetActive(true);
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>Botón "Quit".</summary>
    public void Salir()
    {
        StartCoroutine(SalirCO());
    }

    IEnumerator SalirCO()
    {
        Time.timeScale = 1f;
        yield return Fade(0f, 1f, duracionFade);

        if (GameManager.Instance != null)
            GameManager.Instance.Reiniciar();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        SceneManager.LoadScene(escenaMenu);
    }

    // ── Audio ─────────────────────────────────────────────────────────────
    public void CambiarVolumenMusica(float valor)
    {
        AplicarVolumen(parametroMusica, valor);
        PlayerPrefs.SetFloat(parametroMusica, valor);
    }

    public void CambiarVolumenSFX(float valor)
    {
        AplicarVolumen(parametroSFX, valor);
        PlayerPrefs.SetFloat(parametroSFX, valor);
    }

    void AplicarVolumen(string parametro, float lineal)
    {
        if (audioMixer == null) return;
        float dB = lineal > 0.0001f ? Mathf.Log10(lineal) * 20f : -80f;
        audioMixer.SetFloat(parametro, dB);
    }

    void InicializarSlider(Slider slider, string parametro, UnityEngine.Events.UnityAction<float> callback)
    {
        if (slider == null) return;
        slider.onValueChanged.RemoveAllListeners();
        slider.value = PlayerPrefs.GetFloat(parametro, 1f);
        AplicarVolumen(parametro, slider.value);
        slider.onValueChanged.AddListener(callback);
    }

    // ── Fade (unscaled — funciona con timeScale = 0) ───────────────────────
    IEnumerator Fade(float desde, float hasta, float duracion)
    {
        if (panelFade == null) yield break;
        float t = 0f;
        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
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
