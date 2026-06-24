using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Escena IntroAdvertencia: reproduce un video a pantalla completa,
/// luego carga la escena del menú principal.
/// El jugador puede saltar con click/tap en cualquier momento.
/// </summary>
public class ReproductorIntro : MonoBehaviour
{
    [Header("── Escena destino ───────────────────────")]
    [Tooltip("Nombre exacto de la escena del menú principal")]
    public string escenaMenu = "MenuInicio";

    [Header("── Video ────────────────────────────────")]
    public VideoPlayer videoPlayer;

    [Header("── UI ───────────────────────────────────")]
    [Tooltip("Panel negro para fade in/out (Image que cubre toda la pantalla)")]
    public Image panelFade;

    [Tooltip("Botón opcional para saltar el video")]
    public Button botonSaltar;

    [Header("── Tiempos ─────────────────────────────")]
    public float duracionFade = 0.6f;

    bool _saltar = false;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (botonSaltar != null)
        {
            botonSaltar.onClick.RemoveAllListeners();
            botonSaltar.onClick.AddListener(Saltar);
            botonSaltar.gameObject.SetActive(false);
        }

        StartCoroutine(SecuenciaCO());
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator SecuenciaCO()
    {
        _saltar = false;

        // Arrancar con pantalla negra
        SetAlpha(1f);

        // Preparar video y esperar
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared)
            yield return null;

        // Fade: negro → video
        videoPlayer.Play();
        yield return Fade(1f, 0f, duracionFade);

        // Mostrar botón saltar
        if (botonSaltar != null) botonSaltar.gameObject.SetActive(true);

        // Esperar a que termine el video o el jugador salte con click/tap
        var mouse  = Mouse.current;
        var touchs = Touchscreen.current;

        while (videoPlayer.isPlaying && !_saltar)
        {
            bool click = mouse  != null && mouse.leftButton.wasPressedThisFrame;
            bool tap   = touchs != null && touchs.primaryTouch.press.wasPressedThisFrame;
            if (click || tap) _saltar = true;
            yield return null;
        }

        videoPlayer.Stop();
        if (botonSaltar != null) botonSaltar.gameObject.SetActive(false);

        // Fade: video → negro → cargar menú
        yield return Fade(0f, 1f, duracionFade);
        SceneManager.LoadScene(escenaMenu);
    }

    /// <summary>Llamado por el botón Saltar o puede invocarse por código.</summary>
    public void Saltar() => _saltar = true;

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator Fade(float desde, float hasta, float duracion)
    {
        if (panelFade == null) yield break;
        float t = 0f;
        SetAlpha(desde);
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
