using System.Collections;
using UnityEngine;

/// <summary>
/// Voz/audio de presencia para los NPCs de ambiente en los distintos mapas.
/// Audio 3D espacial — suena desde la posición del NPC en el mundo.
///
/// SETUP EN UNITY:
/// ──────────────────────────────────────────────────────────────────
/// [NPC GameObject]
///   └── SonidoNPCAmbiente.cs
///         ├── tipoNPC          → TipoA / TipoB / TipoC
///         ├── clips[]          → 2–4 clips de voz/murmullo
///         ├── modoReproduccion → Intervalo  ← recomendado
///         ├── intervaloMin     → 5.0
///         ├── intervaloMax     → 10.0
///         ├── volumen          → 0.8
///         └── distanciaMaxima  → 20.0  ← ajusta según el tamaño de tu mapa
/// ──────────────────────────────────────────────────────────────────
/// </summary>
public class SonidoNPCAmbiente : MonoBehaviour
{
    public enum TipoNPC { TipoA, TipoB, TipoC }
    public enum ModoReproduccion { Proximidad, Intervalo, Ambos }

    [Header("── Identificación ──────────────────────")]
    public TipoNPC tipoNPC = TipoNPC.TipoA;

    [Header("── Clips ────────────────────────────────")]
    [Tooltip("Clips de voz/murmullo (se elige uno al azar cada vez)")]
    public AudioClip[] clips;
    [Range(0f, 1f)]
    public float volumen = 0.8f;
    [Range(0f, 0.15f)]
    public float variacionPitch = 0.05f;

    [Header("── Pitch por tipo de NPC ───────────────")]
    [Tooltip("Adulto masculino ~0.9")]
    public float pitchTipoA = 0.9f;
    [Tooltip("Adulto femenino ~1.1")]
    public float pitchTipoB = 1.1f;
    [Tooltip("Niño/a ~1.3")]
    public float pitchTipoC = 1.3f;

    [Header("── Audio 3D ─────────────────────────────")]
    [Tooltip("Distancia mínima donde el volumen es máximo")]
    public float distanciaMinima = 1f;
    [Tooltip("Distancia máxima donde el sonido deja de escucharse — ajusta al tamaño del mapa")]
    public float distanciaMaxima = 20f;

    [Header("── Modo ─────────────────────────────────")]
    public ModoReproduccion modoReproduccion = ModoReproduccion.Intervalo;

    [Header("── Proximidad ───────────────────────────")]
    [Tooltip("Distancia al jugador para activar (modo Proximidad)")]
    public float distanciaActivar = 8f;
    public Transform jugador;
    public float cooldownProximidad = 6f;

    [Header("── Intervalo ────────────────────────────")]
    public float intervaloMin = 5f;
    public float intervaloMax = 10f;

    // ── Estado ────────────────────────────────────────────────────────────
    AudioSource _fuente;
    bool _enCooldown = false;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _fuente = gameObject.AddComponent<AudioSource>();
        _fuente.playOnAwake = false;
        _fuente.loop = false;
        _fuente.spatialBlend = 1f;                          // 3D completo
        _fuente.rolloffMode = AudioRolloffMode.Custom;     // curva más natural que Linear
        _fuente.minDistance = distanciaMinima;
        _fuente.maxDistance = distanciaMaxima;
        _fuente.dopplerLevel = 0f;                          // sin efecto doppler en NPCs estáticos
        _fuente.volume = volumen;

        // Curva de rolloff personalizada: volumen completo cerca, caída suave
        AnimationCurve curva = new AnimationCurve();
        curva.AddKey(0f, 1f);
        curva.AddKey(0.1f, 0.9f);
        curva.AddKey(0.5f, 0.4f);
        curva.AddKey(1f, 0f);
        _fuente.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curva);
    }

    void Start()
    {
        if (jugador == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) jugador = p.transform;
        }

        if (modoReproduccion == ModoReproduccion.Intervalo ||
            modoReproduccion == ModoReproduccion.Ambos)
            StartCoroutine(BucleIntervaloCO());
    }

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (modoReproduccion == ModoReproduccion.Intervalo) return;
        if (_enCooldown || jugador == null) return;
        if (clips == null || clips.Length == 0) return;

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
            ReproducirClipAleatorio();
            yield return new WaitForSeconds(Random.Range(intervaloMin, intervaloMax));
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    void ReproducirClipAleatorio()
    {
        if (clips == null || clips.Length == 0 || _fuente == null) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;

        float pitchBase = tipoNPC == TipoNPC.TipoA ? pitchTipoA :
                          tipoNPC == TipoNPC.TipoB ? pitchTipoB : pitchTipoC;

        _fuente.pitch = pitchBase + Random.Range(-variacionPitch, variacionPitch);
        _fuente.PlayOneShot(clip, volumen);
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>Forzar reproducción desde código externo.</summary>
    public void HablarAhora() => ReproducirClipAleatorio();
}
