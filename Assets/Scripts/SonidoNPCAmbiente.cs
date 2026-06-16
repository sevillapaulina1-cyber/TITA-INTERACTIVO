using System.Collections;
using UnityEngine;

/// <summary>
/// Voz/audio de presencia para los NPCs de ambiente en los distintos mapas
/// (diferente al NPC niño principal). Reproduce un clip de voz/sonido cuando
/// el jugador se acerca, o a intervalos aleatorios para dar vida al escenario.
///
/// TIPOS DE NPC AMBIENTE:
///   TipoNPC.TipoA → NPC tipo 1 (ej. adulto masculino)
///   TipoNPC.TipoB → NPC tipo 2 (ej. adulto femenino)
///   TipoNPC.TipoC → NPC tipo 3 (ej. niño/a secundario)
///
/// SETUP EN UNITY:
/// ──────────────────────────────────────────────────────────────────
/// [NPC GameObject]
///   ├── NPC Mesh / Animator
///   └── SonidoNPCAmbiente.cs
///         ├── tipoNPC            → TipoA / TipoB / TipoC
///         ├── fuenteAudio        → AudioSource (espacial 3D, no loop, no playOnAwake)
///         ├── clips[]            → 2–4 clips de voz/murmullo para este tipo de NPC
///         ├── modoReproduccion   → Proximidad / Intervalo / Ambos
///         ├── distanciaActivar   → 4.0  (solo en modo Proximidad)
///         ├── intervaloMin       → 6.0  (solo en modo Intervalo)
///         ├── intervaloMax       → 12.0
///         └── volumen            → 0.7
/// ──────────────────────────────────────────────────────────────────
/// </summary>
public class SonidoNPCAmbiente : MonoBehaviour
{
    public enum TipoNPC { TipoA, TipoB, TipoC }
    public enum ModoReproduccion { Proximidad, Intervalo, Ambos }

    [Header("── Identificación ──────────────────────")]
    public TipoNPC tipoNPC = TipoNPC.TipoA;

    [Header("── Audio ───────────────────────────────")]
    public AudioSource fuenteAudio;
    [Tooltip("Clips de voz/murmullo para este NPC (se elige uno al azar)")]
    public AudioClip[] clips;
    [Range(0f, 1f)]
    public float volumen = 0.7f;
    [Tooltip("Variación de pitch para cada reproducción")]
    [Range(0f, 0.15f)]
    public float variacionPitch = 0.05f;

    [Header("── Pitch por tipo de NPC ───────────────")]
    [Tooltip("Pitch base para TipoA (adulto masculino ~0.9)")]
    public float pitchTipoA = 0.9f;
    [Tooltip("Pitch base para TipoB (adulto femenino ~1.1)")]
    public float pitchTipoB = 1.1f;
    [Tooltip("Pitch base para TipoC (niño/a ~1.3)")]
    public float pitchTipoC = 1.3f;

    [Header("── Modo ─────────────────────────────────")]
    public ModoReproduccion modoReproduccion = ModoReproduccion.Ambos;

    [Header("── Proximidad ───────────────────────────")]
    [Tooltip("Distancia al jugador para activar (modo Proximidad)")]
    public float distanciaActivar = 4.0f;
    [Tooltip("Referencia al jugador (se busca por Tag si está vacío)")]
    public Transform jugador;
    [Tooltip("Cooldown tras reproducir por proximidad (segundos)")]
    public float cooldownProximidad = 5.0f;

    [Header("── Intervalo ────────────────────────────")]
    [Tooltip("Intervalo mínimo entre reproducciones aleatorias (segundos)")]
    public float intervaloMin = 6.0f;
    [Tooltip("Intervalo máximo entre reproducciones aleatorias (segundos)")]
    public float intervaloMax = 14.0f;

    // ── Estado ────────────────────────────────────────────────────────────
    float _tiempoProximidad = 0f;
    bool _enCooldown = false;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (fuenteAudio == null)
            fuenteAudio = gameObject.AddComponent<AudioSource>();

        fuenteAudio.playOnAwake = false;
        fuenteAudio.loop = false;
        fuenteAudio.spatialBlend = 1f; // 3D — suena desde la posición del NPC
        fuenteAudio.rolloffMode = AudioRolloffMode.Linear;
        fuenteAudio.maxDistance = 12f;
        fuenteAudio.minDistance = 1f;
    }

    void Start()
    {
        // Buscar jugador por tag si no está asignado
        if (jugador == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) jugador = p.transform;
        }

        // Iniciar reproducción por intervalo
        if (modoReproduccion == ModoReproduccion.Intervalo ||
            modoReproduccion == ModoReproduccion.Ambos)
            StartCoroutine(BucleIntervaloCO());
    }

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (modoReproduccion == ModoReproduccion.Intervalo) return;
        if (_enCooldown || jugador == null || clips == null || clips.Length == 0) return;

        float distancia = Vector3.Distance(transform.position, jugador.position);
        if (distancia <= distanciaActivar)
            StartCoroutine(ReproducirConCooldownCO());
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator ReproducirConCooldownCO()
    {
        _enCooldown = true;
        ReproducirClipAleatorio();
        yield return new WaitForSeconds(cooldownProximidad);
        _enCooldown = false;
    }

    IEnumerator BucleIntervaloCO()
    {
        // Delay inicial aleatorio para que los NPCs no suenen todos a la vez
        yield return new WaitForSeconds(Random.Range(0f, intervaloMax));

        while (true)
        {
            if (clips != null && clips.Length > 0 && !fuenteAudio.isPlaying)
                ReproducirClipAleatorio();

            yield return new WaitForSeconds(Random.Range(intervaloMin, intervaloMax));
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    void ReproducirClipAleatorio()
    {
        if (clips == null || clips.Length == 0 || fuenteAudio == null) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;

        float pitchBase = tipoNPC == TipoNPC.TipoA ? pitchTipoA :
                          tipoNPC == TipoNPC.TipoB ? pitchTipoB : pitchTipoC;

        fuenteAudio.pitch = pitchBase + Random.Range(-variacionPitch, variacionPitch);
        fuenteAudio.PlayOneShot(clip, volumen);
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>Forzar reproducción desde código externo.</summary>
    public void HablarAhora() => ReproducirClipAleatorio();
}
