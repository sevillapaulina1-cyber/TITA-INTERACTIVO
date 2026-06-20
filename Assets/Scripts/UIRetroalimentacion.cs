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

    // ── ▼ NUEVO: el texto del botón Saltar se asigna por código, porque en el
    //     build no estaba apareciendo (problema típico de fuente/Text vacío) ──
    [Header("── Texto del botón Saltar ──────────────")]
    [Tooltip("Text (UI) del botón Saltar. Arrástralo aquí; el script le pone el texto en Start().")]
    public Text textoBotonSaltar;
    [Tooltip("Texto que se va a mostrar en el botón Saltar")]
    public string textoSaltar = "Saltar";
    // ── ▲ NUEVO ─────────────────────────────────────────────────────────────

    [Header("── Panel de retroalimentación ─────────")]
    public GameObject panelRetro;

    // ── ▼ NUEVO: pantalla de reflexión, aparece ANTES de la retroalimentación ──
    [Header("── Reflexión (entre cinemática y retro) ─")]
    [Tooltip("Panel propio para los mensajes de reflexión, separado del panel de retro y del mapa")]
    public GameObject panelReflexion;
    [Tooltip("Texto donde se va mostrando cada mensaje de reflexión")]
    public Text textoReflexion;
    [Tooltip("CanvasGroup del panelReflexion, usado para el fundido. Se busca/crea automáticamente si está vacío.")]
    public CanvasGroup canvasGroupReflexion;
    [Tooltip("Mensajes de reflexión que se muestran uno por uno, en orden, antes de la retroalimentación")]
    [TextArea(2, 4)]
    public string[] mensajesReflexion;
    [Tooltip("Segundos que se mantiene visible cada mensaje (sin contar el fundido)")]
    public float duracionPorMensaje = 4f;
    [Tooltip("Duración del fundido de entrada/salida de cada mensaje")]
    public float duracionFadeReflexion = 0.8f;
    // ── ▲ NUEVO ─────────────────────────────────────────────────────────────

    [Header("── Textos de retroalimentación ────────")]
    public Text textoResumen;
    public Text textoConfianza;
    public Text textoRiesgo;
    public Text textoFinal;

    [Header("── Botones ─────────────────────────────")]
    public Button botonSaltarBtn;
    public Button botonReiniciarBtn;

    [Header("── Reinicio ────────────────────────────")]
    public string escenaInicio = "NIVEL1";

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

        // ── ▼ NUEVO ──────────────────────────────────────────────────────
        if (panelReflexion != null)
            panelReflexion.SetActive(false);
        // ── ▲ NUEVO ──────────────────────────────────────────────────────

        if (botonSaltar != null)
            botonSaltar.SetActive(true);

        // ── ▼ NUEVO: forzar el texto del botón Saltar por código ───────────
        if (textoBotonSaltar == null && botonSaltar != null)
            textoBotonSaltar = botonSaltar.GetComponentInChildren<Text>(true);

        if (textoBotonSaltar != null)
            textoBotonSaltar.text = textoSaltar;
        // ── ▲ NUEVO ──────────────────────────────────────────────────────

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
            yield return SecuenciaReflexionYRetroCO();
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

        yield return SecuenciaReflexionYRetroCO();
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
        StartCoroutine(SecuenciaReflexionYRetroCO());
    }

    // ─────────────────────────────────────────────────────────────────────
    // ── ▼ NUEVO: oculta video/skip, muestra los mensajes de reflexión
    //     (pantalla propia, sin el mapa) y luego pasa a la retroalimentación ──
    IEnumerator SecuenciaReflexionYRetroCO()
    {
        if (videoScreen != null)
            videoScreen.gameObject.SetActive(false);

        if (botonSaltar != null)
            botonSaltar.SetActive(false);

        yield return MostrarReflexionCO();

        MostrarPantallaRetro();
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator MostrarReflexionCO()
    {
        if (panelReflexion == null || textoReflexion == null || mensajesReflexion == null || mensajesReflexion.Length == 0)
            yield break;

        CanvasGroup cg = canvasGroupReflexion;
        if (cg == null) cg = panelReflexion.GetComponent<CanvasGroup>();
        if (cg == null) cg = panelReflexion.AddComponent<CanvasGroup>();

        panelReflexion.SetActive(true);

        foreach (string mensaje in mensajesReflexion)
        {
            if (string.IsNullOrEmpty(mensaje)) continue;

            textoReflexion.text = mensaje;

            yield return FundirCanvasGroupCO(cg, 0f, 1f, duracionFadeReflexion);
            yield return new WaitForSeconds(duracionPorMensaje);
            yield return FundirCanvasGroupCO(cg, 1f, 0f, duracionFadeReflexion);
        }

        panelReflexion.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator FundirCanvasGroupCO(CanvasGroup cg, float desde, float hasta, float duracion)
    {
        float t = 0f;
        cg.alpha = desde;
        while (t < duracion)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(desde, hasta, t / duracion);
            yield return null;
        }
        cg.alpha = hasta;
    }
    // ── ▲ NUEVO ─────────────────────────────────────────────────────────────

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
            textoFinal.text = $"{gm.ObtenerTituloFinal()}\n{gm.ObtenerMensajeFinal()}";
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

