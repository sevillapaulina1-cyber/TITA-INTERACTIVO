using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

/// <summary>
/// Coloca este script en la escena de Final1 o Final2.
///
/// FLUJO:
///   1. La escena carga → se oculta el panel de retroalimentación
///   2. Se reproduce el video .mp4 en pantalla completa
///   3. Al terminar el video (o si el jugador lo salta) → aparece la retroalimentación
///
/// SETUP EN INSPECTOR:
///   - VideoPlayer:  componente VideoPlayer en cualquier GameObject de la escena
///   - RawImage:     RawImage que cubre toda la pantalla (Canvas en Screen Space - Overlay)
///                   asígnale el RenderTexture del VideoPlayer como textura
///   - PanelRetro:   el GameObject raíz del panel de retroalimentación (empieza desactivado)
///   - BotonSaltar:  botón opcional "Saltar video" visible durante la reproducción
/// </summary>
public class UIRetroalimentacion : MonoBehaviour
{
    [Header("── Video ──────────────────────────────")]
    public VideoPlayer videoPlayer;         // Componente VideoPlayer de la escena
    public RawImage   videoScreen;          // RawImage que muestra el video (pantalla completa)
    public GameObject botonSaltar;          // Botón "Saltar" (opcional, puede ser null)

    [Header("── Panel de retroalimentación ─────────")]
    public GameObject panelRetro;           // Panel completo (se activa al terminar el video)

    [Header("── Textos de retroalimentación ────────")]
    public Text textoResumen;               // Resumen completo multilinea
    public Text textoConfianza;             // Ej: "Confianza: 32 pts"
    public Text textoRiesgo;                // Ej: "Riesgo: 0 pts"
    public Text textoFinal;                 // Ej: "Final 1 — Secuestro"

    [Header("── Reinicio ────────────────────────────")]
    public string escenaInicio = "Inicio";

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        // Ocultar retroalimentación hasta que termine el video
        if (panelRetro != null)
            panelRetro.SetActive(false);

        if (botonSaltar != null)
            botonSaltar.SetActive(true);

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

        // Preparar video y esperar a que esté listo
        videoPlayer.Prepare();
        yield return new WaitUntil(() => videoPlayer.isPrepared);

        videoPlayer.Play();

        // Esperar a que el video termine
        yield return new WaitUntil(() =>
            !videoPlayer.isPlaying ||
            (videoPlayer.frameCount > 0 &&
             videoPlayer.frame >= (long)videoPlayer.frameCount - 2)
        );

        MostrarPantallaRetro();
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Llamado por el botón "Saltar" en el Inspector (OnClick).
    /// </summary>
    public void SaltarVideo()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Stop();

        StopAllCoroutines();
        MostrarPantallaRetro();
    }

    // ─────────────────────────────────────────────────────────────────────
    void MostrarPantallaRetro()
    {
        // Ocultar video
        if (videoScreen != null)
            videoScreen.gameObject.SetActive(false);

        if (botonSaltar != null)
            botonSaltar.SetActive(false);

        // Mostrar panel de retroalimentación
        if (panelRetro != null)
            panelRetro.SetActive(true);

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

        if (textoResumen   != null) textoResumen.text   = gm.ObtenerResumen();
        if (textoConfianza != null) textoConfianza.text = $"Confianza: {gm.PuntosConfianza} pts";
        if (textoRiesgo    != null) textoRiesgo.text    = $"Riesgo:    {gm.PuntosRiesgo} pts";

        if (textoFinal != null)
        {
            bool esFinal1 = gm.PuntosConfianza >= gm.PuntosRiesgo;
            textoFinal.text = esFinal1 ? "Final 1 — Secuestro" : "Final 2 — Policía";
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Asigna al botón de reinicio en el Inspector (OnClick).
    /// </summary>
    public void ReiniciarExperiencia()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.Reiniciar();

        SceneManager.LoadScene(escenaInicio);
    }
}
