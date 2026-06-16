using System.Collections;
using UnityEngine;

/// <summary>
/// Reproduce sonidos de voz tipo "murmullo" para un NPC durante el diálogo.
/// Añade este componente al mismo GameObject que SistemaDialogo o DialogoCelular.
///
/// COMPORTAMIENTO:
///   - Reproduce un clip corto de "voz" de manera repetida mientras el NPC habla
///     (simula el efecto Animal Crossing / VN de voz burbuja).
///   - También puede reproducir un clip de "notificación" para mensajes entrantes.
///
/// SETUP EN UNITY:
/// ──────────────────────────────────────────────────────────────────
/// [GameObject con SistemaDialogo]
///   └── SonidoNPC.cs
///         ├── fuenteVoz          → AudioSource en el mismo GO (no loop, no playOnAwake)
///         ├── clipVozNPC         → clip corto de murmullo / voz del niño (~0.08s)
///         ├── clipNotificacion   → sonido de mensaje entrante (celular)
///         ├── clipMensajeEnviado → sonido de mensaje saliente (celular)
///         ├── intervaloVoz       → 0.07  (ajusta para que suene natural)
///         └── volumenVoz         → 0.6
///
/// USO DESDE CÓDIGO:
///   SonidoNPC sonido = GetComponent<SonidoNPC>();
///   sonido.HablarNPC();          // inicia la "voz" del NPC
///   sonido.PararVoz();           // detiene la voz
///   sonido.TocarNotificacion();  // notificación de mensaje entrante
///   sonido.TocarMensajeEnviado();// sonido al enviar un mensaje
/// ──────────────────────────────────────────────────────────────────
/// </summary>
public class SonidoNPC : MonoBehaviour
{
    [Header("── Fuente de audio ──────────────────────")]
    public AudioSource fuenteVoz;

    [Header("── Clips ────────────────────────────────")]
    [Tooltip("Clip corto de murmullo/voz del NPC (ej. 0.05–0.15s)")]
    public AudioClip clipVozNPC;
    [Tooltip("Sonido de notificación al recibir mensaje (celular)")]
    public AudioClip clipNotificacion;
    [Tooltip("Sonido al enviar un mensaje (celular)")]
    public AudioClip clipMensajeEnviado;

    [Header("── Configuración ───────────────────────")]
    [Tooltip("Intervalo entre cada sílaba de voz (segundos)")]
    [Range(0.03f, 0.2f)]
    public float intervaloVoz = 0.07f;
    [Range(0f, 1f)]
    public float volumenVoz = 0.6f;
    [Tooltip("Variación de pitch para efecto más natural")]
    [Range(0f, 0.3f)]
    public float variacionPitch = 0.1f;
    [Tooltip("Pitch base de la voz del NPC")]
    [Range(0.5f, 2.0f)]
    public float pitchBase = 1.2f;   // más agudo = voz más joven

    // ── Estado ────────────────────────────────────────────────────────────
    bool _hablando = false;
    Coroutine _coroutineVoz;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        // Auto-crear AudioSource si no se asignó
        if (fuenteVoz == null)
            fuenteVoz = gameObject.AddComponent<AudioSource>();

        fuenteVoz.playOnAwake = false;
        fuenteVoz.loop = false;
        fuenteVoz.spatialBlend = 0f; // 2D
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Inicia el bucle de voz del NPC.
    /// Llamar al inicio de cada línea del NPC.
    /// </summary>
    public void HablarNPC()
    {
        if (clipVozNPC == null || _hablando) return;
        _hablando = true;
        if (_coroutineVoz != null) StopCoroutine(_coroutineVoz);
        _coroutineVoz = StartCoroutine(BucleVozCO());
    }

    /// <summary>
    /// Detiene la voz del NPC.
    /// Llamar cuando termina de escribirse el texto del NPC.
    /// </summary>
    public void PararVoz()
    {
        _hablando = false;
        if (_coroutineVoz != null) { StopCoroutine(_coroutineVoz); _coroutineVoz = null; }
        if (fuenteVoz != null && fuenteVoz.isPlaying) fuenteVoz.Stop();
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Reproduce el sonido de notificación de mensaje entrante.
    /// </summary>
    public void TocarNotificacion()
    {
        if (clipNotificacion == null || fuenteVoz == null) return;
        fuenteVoz.pitch = 1f;
        fuenteVoz.PlayOneShot(clipNotificacion, volumenVoz);
    }

    /// <summary>
    /// Reproduce el sonido de mensaje enviado.
    /// </summary>
    public void TocarMensajeEnviado()
    {
        if (clipMensajeEnviado == null || fuenteVoz == null) return;
        fuenteVoz.pitch = 1f;
        fuenteVoz.PlayOneShot(clipMensajeEnviado, volumenVoz * 0.8f);
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator BucleVozCO()
    {
        while (_hablando)
        {
            if (fuenteVoz != null && clipVozNPC != null)
            {
                // Variación de pitch para sonido más natural
                fuenteVoz.pitch = pitchBase + Random.Range(-variacionPitch, variacionPitch);
                fuenteVoz.PlayOneShot(clipVozNPC, volumenVoz);
            }
            yield return new WaitForSeconds(intervaloVoz);
        }
    }
}
