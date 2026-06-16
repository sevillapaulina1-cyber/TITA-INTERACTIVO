using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// Reproduce el video de introducción (.mp4) y al terminar carga la escena del juego.
/// MODIFICADO: Añade sonido al botón "Saltar".
/// </summary>
public class CinematicaIntro : MonoBehaviour
{
    [Header("── Video ───────────────────────────────")]
    public VideoPlayer videoPlayer;
    public RawImage videoScreen;

    [Header("── Botón saltar (opcional) ────────────")]
    public GameObject botonSaltar;
    public Button botonSaltarBtn; // ← asigna el Button del botonSaltar

    [Header("── Siguiente escena ───────────────────")]
    [Tooltip("Nombre exacto de tu escena principal de juego")]
    public string escenaJuego = "EscenaPrincipal";

    // ── ▼ AUDIO (NUEVO) ──────────────────────────────────────────────────
    [Header("── Audio UI ────────────────────────────")]
    public SonidoUI sonidoUI;
    // ── ▲ AUDIO ──────────────────────────────────────────────────────────

    bool _saltado = false;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (botonSaltar != null)
            botonSaltar.SetActive(true);

        // Buscar SonidoUI y registrar botón
        if (sonidoUI == null)
            sonidoUI = FindAnyObjectByType<SonidoUI>();

        // ── ▼ AUDIO: registrar botón skip (NUEVO) ────────────────────────
        if (sonidoUI != null && botonSaltarBtn != null)
            sonidoUI.RegistrarBoton(botonSaltarBtn, SonidoUI.TipoSonidoBtn.Skip);
        // ── ▲ AUDIO ──────────────────────────────────────────────────────

        StartCoroutine(ReproducirCO());
    }

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (_saltado) return;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            SaltarVideo();
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            SaltarVideo();
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator ReproducirCO()
    {
        if (videoPlayer == null || videoScreen == null)
        {
            Debug.LogWarning("[CinematicaIntro] Falta VideoPlayer o RawImage — cargando juego directo.");
            CargarJuego();
            yield break;
        }

        videoScreen.gameObject.SetActive(true);

        videoPlayer.Prepare();
        yield return new WaitUntil(() => videoPlayer.isPrepared);

        videoPlayer.Play();

        yield return new WaitUntil(() =>
            _saltado ||
            !videoPlayer.isPlaying ||
            (videoPlayer.frameCount > 0 &&
             videoPlayer.frame >= (long)videoPlayer.frameCount - 2)
        );

        if (!_saltado)
            CargarJuego();
    }

    // ─────────────────────────────────────────────────────────────────────
    public void SaltarVideo()
    {
        if (_saltado) return;
        _saltado = true;

        // ── ▼ AUDIO: sonido de skip (NUEVO) ──────────────────────────────
        SonidoUI.TocarSkip();
        // ── ▲ AUDIO ──────────────────────────────────────────────────────

        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Stop();

        StopAllCoroutines();
        CargarJuego();
    }

    // ─────────────────────────────────────────────────────────────────────
    void CargarJuego()
    {
        if (botonSaltar != null)
            botonSaltar.SetActive(false);

        SceneManager.LoadScene(escenaJuego);
    }
}
