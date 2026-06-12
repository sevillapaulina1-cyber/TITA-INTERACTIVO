using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

/// <summary>

/// </summary>
public class MenuPausa : MonoBehaviour
{
    [Header("── Escenas ──────────────────────────────")]
    public string escenaMenu = "MenuInicio";

    [Header("── Panels ───────────────────────────────")]
    public GameObject panelPausa;
    public GameObject panelBotones;

    [Header("── Fade ─────────────────────────────────")]
    public Image panelFade;
    public float duracionFade = 0.8f;

    [Header("── Audio ───────────────────────────────")]
    public AudioMixer audioMixer;
    public string parametroVolumen = "VolMaster";
    public Slider sliderVolumen;

    [Header("── Jugador ──────────────────────────────")]
    public MonoBehaviour firstPersonController;

    // ── Estado ────────────────────────────────────────────────────────────
    bool _pausado = false;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (panelPausa != null) panelPausa.SetActive(false);
        if (panelFade != null) SetAlpha(0f);

        InicializarSlider();
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
    /// <summary>Botón "Resume" y ESC cuando está pausado.</summary>
    public void Continuar()
    {
        _pausado = false;
        Time.timeScale = 1f;

        if (panelPausa != null) panelPausa.SetActive(false);

        if (firstPersonController != null)
            firstPersonController.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>Botón "Quit" — fade a negro y carga MenuInicio.</summary>
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
        Cursor.visible = true;

        SceneManager.LoadScene(escenaMenu);
    }

    // ── Audio ─────────────────────────────────────────────────────────────
    /// <summary>Llamado por OnValueChanged del SliderVolumen.</summary>
    public void CambiarVolumen(float valor)
    {
        AplicarVolumen(valor);
        PlayerPrefs.SetFloat(parametroVolumen, valor);
    }

    void AplicarVolumen(float lineal)
    {
        if (audioMixer == null) return;
        // Convierte lineal [0,1] a decibelios [-80, 0]
        float dB = lineal > 0.0001f ? Mathf.Log10(lineal) * 20f : -80f;
        audioMixer.SetFloat(parametroVolumen, dB);
    }

    void InicializarSlider()
    {
        if (sliderVolumen == null) return;
        sliderVolumen.onValueChanged.RemoveAllListeners();
        // Recupera el volumen guardado (1 = máximo por defecto)
        sliderVolumen.value = PlayerPrefs.GetFloat(parametroVolumen, 1f);
        AplicarVolumen(sliderVolumen.value);
        sliderVolumen.onValueChanged.AddListener(CambiarVolumen);
    }

    // ── Fade (unscaled — funciona con timeScale = 0) ──────────────────────
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
