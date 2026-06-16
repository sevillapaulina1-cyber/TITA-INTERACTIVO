using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

/// <summary>
/// Menú de pausa.
/// El slider de volumen controla el parámetro "VolMaster" del AudioMixer raíz,
/// que debe ser el mismo AudioMixer asignado en AudioManager (MixerPrincipal).
/// Así el slider afecta TODA la salida de audio: música, SFX y respiración.
///
/// SETUP DEL MIXER EN UNITY:
///   1. Selecciona el MixerPrincipal en el Project panel.
///   2. En la ventana Audio Mixer, haz clic derecho en el volumen del grupo "Master"
///      → "Expose 'Volume' to script" → renómbralo "VolMaster".
///   3. Asigna ese mismo MixerPrincipal al campo audioMixer de este script
///      Y al campo audioMixer del AudioManager.
/// </summary>
public class MenuPausa : MonoBehaviour
{
    [Header("── Escenas ──────────────────────────────")]
    public string escenaMenu = "MenuInicio";

    [Header("── Panels ───────────────────────────────")]
    public GameObject panelPausa;
    public GameObject panelBotones;

    [Header("── Botones ─────────────────────────────")]
    public Button botonContinuar;
    public Button botonSalir;

    [Header("── Fade ─────────────────────────────────")]
    public Image panelFade;
    public float duracionFade = 0.8f;

    [Header("── Audio Mixer (slider de volumen) ───────")]
    [Tooltip("El mismo MixerPrincipal que usa AudioManager")]
    public AudioMixer audioMixer;
    [Tooltip("Nombre del parámetro expuesto del Master (clic derecho → Expose → renombrar)")]
    public string parametroVolumen = "VolMaster";
    public Slider sliderVolumen;
    public Text textoVolumen;
    public string etiquetaVolumen = "Volumen";

    [Header("── Jugador ──────────────────────────────")]
    public MonoBehaviour firstPersonController;

    [Header("── Audio UI ────────────────────────────")]
    public SonidoUI sonidoUI;

    bool _pausado = false;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (panelPausa != null) panelPausa.SetActive(false);
        if (panelFade != null) SetAlpha(0f);
        if (textoVolumen != null) textoVolumen.text = etiquetaVolumen;

        InicializarSlider();

        if (sonidoUI == null)
            sonidoUI = FindAnyObjectByType<SonidoUI>();

        if (sonidoUI != null)
        {
            if (botonContinuar != null) sonidoUI.RegistrarBoton(botonContinuar, SonidoUI.TipoSonidoBtn.Click);
            if (botonSalir != null) sonidoUI.RegistrarBoton(botonSalir, SonidoUI.TipoSonidoBtn.Click);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (_pausado) Continuar();
            else Pausar();
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    void Pausar()
    {
        _pausado = true;
        Time.timeScale = 0f;

        if (firstPersonController != null) firstPersonController.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (panelPausa != null) panelPausa.SetActive(true);
        if (panelBotones != null) panelBotones.SetActive(true);
    }

    // ─────────────────────────────────────────────────────────────────────
    public void Continuar()
    {
        SonidoUI.TocarClick();

        _pausado = false;
        Time.timeScale = 1f;

        if (panelPausa != null) panelPausa.SetActive(false);
        if (firstPersonController != null) firstPersonController.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ─────────────────────────────────────────────────────────────────────
    public void Salir()
    {
        SonidoUI.TocarClick();
        StartCoroutine(SalirCO());
    }

    IEnumerator SalirCO()
    {
        Time.timeScale = 1f;
        yield return Fade(0f, 1f, duracionFade);

        if (GameManager.Instance != null)
            GameManager.Instance.Reiniciar();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(escenaMenu);
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Llamado por OnValueChanged del Slider.
    /// Convierte valor lineal [0,1] a decibelios y lo aplica al Master del Mixer.
    /// Esto afecta TODA la salida de audio (música + SFX) porque es el grupo raíz.
    /// </summary>
    public void CambiarVolumen(float valor)
    {
        AplicarVolumen(valor);
        PlayerPrefs.SetFloat(parametroVolumen, valor);
    }

    void AplicarVolumen(float lineal)
    {
        if (audioMixer == null) return;
        // Conversión lineal → dB. Si es 0 va a -80 dB (silencio).
        float dB = lineal > 0.0001f ? Mathf.Log10(lineal) * 20f : -80f;
        audioMixer.SetFloat(parametroVolumen, dB);
    }

    void InicializarSlider()
    {
        if (sliderVolumen == null) return;
        sliderVolumen.onValueChanged.RemoveAllListeners();
        float valorGuardado = PlayerPrefs.GetFloat(parametroVolumen, 1f);
        sliderVolumen.value = valorGuardado;
        AplicarVolumen(valorGuardado);
        sliderVolumen.onValueChanged.AddListener(CambiarVolumen);
    }

    // ─────────────────────────────────────────────────────────────────────
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
