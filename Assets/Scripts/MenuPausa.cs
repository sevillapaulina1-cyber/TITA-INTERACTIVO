using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

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

    [Header("── Audio ───────────────────────────────")]
    public AudioMixer audioMixer;
    public string parametroVolumen = "VolMaster";
    public Slider sliderVolumen;
    [Tooltip("Text que muestra la etiqueta 'Volumen' encima del slider")]
    public Text textoVolumen;
    public string etiquetaVolumen = "Volumen";

    [Header("── Jugador ──────────────────────────────")]
    public MonoBehaviour firstPersonController;

    // ── ▼ AUDIO UI (NUEVO) ───────────────────────────────────────────────
    [Header("── Audio UI ────────────────────────────")]
    [Tooltip("Componente SonidoUI en el Canvas (se busca automáticamente)")]
    public SonidoUI sonidoUI;
    // ── ▲ AUDIO UI ───────────────────────────────────────────────────────

    bool _pausado = false;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (panelPausa != null) panelPausa.SetActive(false);
        if (panelFade != null) SetAlpha(0f);

        if (textoVolumen != null) textoVolumen.text = etiquetaVolumen;
        InicializarSlider();

        // Buscar SonidoUI automáticamente
        if (sonidoUI == null)
            sonidoUI = FindAnyObjectByType<SonidoUI>();

        // ── ▼ AUDIO: registrar botones (NUEVO) ───────────────────────────
        if (sonidoUI != null)
        {
            if (botonContinuar != null) sonidoUI.RegistrarBoton(botonContinuar, SonidoUI.TipoSonidoBtn.Click);
            if (botonSalir != null) sonidoUI.RegistrarBoton(botonSalir, SonidoUI.TipoSonidoBtn.Click);
        }
        // ── ▲ AUDIO ──────────────────────────────────────────────────────
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

        if (firstPersonController != null)
            firstPersonController.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (panelPausa != null) panelPausa.SetActive(true);
        if (panelBotones != null) panelBotones.SetActive(true);
    }

    // ─────────────────────────────────────────────────────────────────────
    public void Continuar()
    {
        // ── ▼ AUDIO: sonido de botón (NUEVO) ─────────────────────────────
        SonidoUI.TocarClick();
        // ── ▲ AUDIO ──────────────────────────────────────────────────────

        _pausado = false;
        Time.timeScale = 1f;

        if (panelPausa != null) panelPausa.SetActive(false);

        if (firstPersonController != null)
            firstPersonController.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ─────────────────────────────────────────────────────────────────────
    public void Salir()
    {
        // ── ▼ AUDIO: sonido de botón (NUEVO) ─────────────────────────────
        SonidoUI.TocarClick();
        // ── ▲ AUDIO ──────────────────────────────────────────────────────

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

    // ── Audio ─────────────────────────────────────────────────────────────
    public void CambiarVolumen(float valor)
    {
        AplicarVolumen(valor);
        PlayerPrefs.SetFloat(parametroVolumen, valor);
    }

    void AplicarVolumen(float lineal)
    {
        if (audioMixer == null) return;
        float dB = lineal > 0.0001f ? Mathf.Log10(lineal) * 20f : -80f;
        audioMixer.SetFloat(parametroVolumen, dB);
    }

    void InicializarSlider()
    {
        if (sliderVolumen == null) return;
        sliderVolumen.onValueChanged.RemoveAllListeners();

        const float VOLUMEN_DEFAULT = 0.7f;
        if (!PlayerPrefs.HasKey(parametroVolumen))
            PlayerPrefs.SetFloat(parametroVolumen, VOLUMEN_DEFAULT);

        float volGuardado = PlayerPrefs.GetFloat(parametroVolumen, VOLUMEN_DEFAULT);
        sliderVolumen.value = volGuardado;
        AplicarVolumen(volGuardado);

        sliderVolumen.onValueChanged.AddListener(CambiarVolumen);
    }

    // ── Fade (unscaled) ───────────────────────────────────────────────────
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
