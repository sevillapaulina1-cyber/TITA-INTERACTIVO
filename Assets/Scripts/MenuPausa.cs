using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// Menú de pausa.
/// El control de volumen fue eliminado — todo el audio se gestiona
/// exclusivamente a través de AudioManager y su AudioMixer.
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
