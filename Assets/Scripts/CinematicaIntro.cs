using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// Reproduce el video de introducción (.mp4) y al terminar carga la escena del juego.
/// El jugador puede saltarlo con SPACE, clic izquierdo o el botón "Saltar".
///
/// SETUP EN UNITY — Jerarquía completa de la escena "Cinematica":
/// ──────────────────────────────────────────────────────────────
/// [Scene: Cinematica]
///   │
///   ├── Main Camera
///   │
///   ├── VideoPlayerGO                    ← GameObject vacío
///   │     └── VideoPlayer (componente)
///   │           ├── Play On Awake  → OFF   (el script lo controla)
///   │           ├── Render Mode    → Render Texture
///   │           ├── Target Texture → VideoRT   (ver paso 1)
///   │           └── Video Clip     → tu .mp4 de intro
///   │
///   ├── Canvas                           ← Screen Space - Overlay, Sort Order 10
///   │     │
///   │     ├── VideoRawImage              ← RawImage
///   │     │     ├── Anchor  → Stretch completo (Alt+clic en anchor presets)
///   │     │     └── Texture → VideoRT
///   │     │
///   │     └── BotonSaltar                ← Button (esquina inferior derecha)
///   │           ├── Width ~120, Height ~40
///   │           ├── Text hijo → "Saltar ▶"
///   │           └── OnClick → CinematicaManager → SaltarVideo()
///   │
///   └── CinematicaManager                ← GameObject vacío
///         └── CinematicaIntro.cs  ← ESTE SCRIPT
///               ├── videoPlayer    → VideoPlayerGO (componente VideoPlayer)
///               ├── videoScreen    → VideoRawImage
///               ├── botonSaltar    → BotonSaltar
///               └── escenaJuego    → "EscenaPrincipal"
///
/// PASO 1 — Crear RenderTexture:
///   Project panel → clic derecho → Create → Render Texture
///   Nombre: "VideoRT" | Size: 1920 × 1080
///   Asignarla al VideoPlayer (Target Texture) y a la RawImage (Texture)
///
/// BUILD SETTINGS — orden de escenas:
///   0. MenuInicio
///   1. Cinematica        ← esta escena
///   2. EscenaPrincipal
///   3. Final1_Secuestro
///   4. Final2_Policia
/// ──────────────────────────────────────────────────────────────
/// </summary>
public class CinematicaIntro : MonoBehaviour
{
    [Header("── Video ───────────────────────────────")]
    public VideoPlayer videoPlayer;     // componente VideoPlayer de la escena
    public RawImage    videoScreen;     // RawImage que muestra el video

    [Header("── Botón saltar (opcional) ────────────")]
    public GameObject  botonSaltar;     // puede quedar null si no quieres botón

    [Header("── Siguiente escena ───────────────────")]
    [Tooltip("Nombre exacto de tu escena principal de juego")]
    public string escenaJuego = "EscenaPrincipal";

    // ── Estado ────────────────────────────────────────────────────────────
    bool _saltado = false;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (botonSaltar != null)
            botonSaltar.SetActive(true);

        StartCoroutine(ReproducirCO());
    }

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (_saltado) return;

        // Saltar con SPACE o clic izquierdo
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

        // Preparar y esperar
        videoPlayer.Prepare();
        yield return new WaitUntil(() => videoPlayer.isPrepared);

        videoPlayer.Play();

        // Esperar a que termine el video
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
    /// <summary>
    /// Llamado por el botón "Saltar" en el Inspector (OnClick),
    /// o automáticamente por SPACE / clic.
    /// </summary>
    public void SaltarVideo()
    {
        if (_saltado) return;
        _saltado = true;

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
