using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

/// <summary>
/// Pantalla de retroalimentación final.
/// MODIFICADO: Inicia música de retroalimentación al mostrar el panel.
///             Añade sonidos a botones Skip y Reiniciar.
/// </summary>
public class UIRetroalimentacion : MonoBehaviour
{
    [Header("── Video ──────────────────────────────")]
    public VideoPlayer videoPlayer;
    public RawImage videoScreen;
    public GameObject botonSaltar;

    [Header("── Panel de retroalimentación ─────────")]
    public GameObject panelRetro;

    [Header("── Textos de retroalimentación ────────")]
    public Text textoResumen;
    public Text textoConfianza;
    public Text textoRiesgo;
    public Text textoFinal;

    [Header("── Botones ─────────────────────────────")]
    public Button botonSaltarBtn;
    public Button botonReiniciarBtn;

    [Header("── Reinicio ────────────────────────────")]
    public string escenaInicio = "Inicio";

    // ── ▼ AUDIO (NUEVO) ──────────────────────────────────────────────────
    [Header("── Audio UI ────────────────────────────")]
    [Tooltip("SonidoUI del Canvas (se busca automáticamente)")]
    public SonidoUI sonidoUI;
    // ── ▲ AUDIO ──────────────────────────────────────────────────────────

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (panelRetro != null)
            panelRetro.SetActive(false);

        if (botonSaltar != null)
            botonSaltar.SetActive(true);

        // Buscar SonidoUI
        if (sonidoUI == null)
            sonidoUI = FindAnyObjectByType<SonidoUI>();

        // ── ▼ AUDIO: registrar botones (NUEVO) ───────────────────────────
        if (sonidoUI != null)
        {
            if (botonSaltarBtn != null) sonidoUI.RegistrarBoton(botonSaltarBtn, SonidoUI.TipoSonidoBtn.Skip);
            if (botonReiniciarBtn != null) sonidoUI.RegistrarBoton(botonReiniciarBtn, SonidoUI.TipoSonidoBtn.Reiniciar);
        }
        // ── ▲ AUDIO ──────────────────────────────────────────────────────

        // Detener música de juego/tensión al entrar a la escena de final
        // (la música de retro empezará al terminar el video)
        if (AudioManager.Instance != null)
            AudioManager.Instance.DetenerMusicaJuego();

        StartCoroutine(ReproducirVideoCO());
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator ReproducirVideoCO()
    {
        if (videoPlayer == null || videoScreen == null)
        {
            Debug.LogWarning("[UIRetroalimentacion] Falta VideoPlayer o RawImage. Se salta el video.");
            MostrarPantallaRetro();
            yield break;
        }

        videoScreen.gameObject.SetActive(true);

        videoPlayer.Prepare();
        yield return new WaitUntil(() => videoPlayer.isPrepared);

        videoPlayer.Play();

        yield return new WaitUntil(() =>
            !videoPlayer.isPlaying ||
            (videoPlayer.frameCount > 0 &&
             videoPlayer.frame >= (long)videoPlayer.frameCount - 2)
        );

        MostrarPantallaRetro();
    }

    // ─────────────────────────────────────────────────────────────────────
    public void SaltarVideo()
    {
        // ── ▼ AUDIO: sonido skip (NUEVO) ─────────────────────────────────
        SonidoUI.TocarSkip();
        // ── ▲ AUDIO ──────────────────────────────────────────────────────

        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Stop();

        StopAllCoroutines();
        MostrarPantallaRetro();
    }

    // ─────────────────────────────────────────────────────────────────────
    void MostrarPantallaRetro()
    {
        if (videoScreen != null)
            videoScreen.gameObject.SetActive(false);

        if (botonSaltar != null)
            botonSaltar.SetActive(false);

        if (panelRetro != null)
            panelRetro.SetActive(true);

        // ── ▼ AUDIO: iniciar música de retroalimentación (NUEVO) ─────────
        if (AudioManager.Instance != null)
            AudioManager.Instance.IniciarMusicaRetro();
        // ── ▲ AUDIO ──────────────────────────────────────────────────────

        RellenarTextos();
    }

    // ─────────────────────────────────────────────────────────────────────
    void RellenarTextos()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[UIRetroalimentacion] GameManager no encontrado.");
            return;
        }

        GameManager gm = GameManager.Instance;

        if (textoResumen != null) textoResumen.text = gm.ObtenerResumen();
        if (textoConfianza != null) textoConfianza.text = $"Confianza: {gm.PuntosConfianza} pts";
        if (textoRiesgo != null) textoRiesgo.text = $"Riesgo:    {gm.PuntosRiesgo} pts";

        if (textoFinal != null)
        {
            bool esFinal1 = gm.PuntosConfianza >= gm.PuntosRiesgo;
            textoFinal.text = esFinal1 ? "Final 1 — Secuestro" : "Final 2 — Policía";
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    public void ReiniciarExperiencia()
    {
        // ── ▼ AUDIO: sonido reiniciar (NUEVO) ────────────────────────────
        SonidoUI.TocarReiniciar();
        // ── ▲ AUDIO ──────────────────────────────────────────────────────

        if (GameManager.Instance != null)
            GameManager.Instance.Reiniciar();

        SceneManager.LoadScene(escenaInicio);
    }
}
