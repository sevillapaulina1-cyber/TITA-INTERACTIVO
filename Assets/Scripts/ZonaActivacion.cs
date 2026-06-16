using System.Collections;
using UnityEngine;

/// <summary>
/// Va en cada zona del suelo. Cuando el jugador la pisa se activa
/// y notifica al GestorZonas.
/// MODIFICADO: Añade sonido al pisar la zona.
/// MODIFICADO: Apaga la luz de la zona al activarse.
/// </summary>
public class ZonaActivacion : MonoBehaviour
{
    [Header("── Gestor ───────────────────────────────")]
    public GestorZonas gestorZonas;

    [Header("── Visual feedback (opcional) ──────────")]
    public Renderer modeloZona;
    public Material materialInactivo;
    public Material materialActivo;

    [Header("── Audio ───────────────────────────────")]
    [Tooltip("AudioSource para el sonido de zona (se crea automáticamente si está vacío)")]
    public AudioSource fuenteAudio;
    [Tooltip("Clip que suena al pisar la zona")]
    public AudioClip clipZona;
    [Range(0f, 1f)]
    public float volumenZona = 0.85f;

    // ── ▼ LUZ (NUEVO) ────────────────────────────────────────────────────
    [Header("── Luz de zona ─────────────────────────")]
    [Tooltip("Light que ilumina la zona. Se apaga al pisarla.")]
    public Light luzZona;

    [Tooltip("Si está marcado, la luz se apaga gradualmente en vez de instantáneo")]
    public bool apagarConFade = false;

    [Tooltip("Duración del fade en segundos (solo si apagarConFade está activo)")]
    [Range(0.1f, 2f)]
    public float duracionFade = 0.5f;
    // ── ▲ LUZ ────────────────────────────────────────────────────────────

    bool _activada = false;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        // Auto-crear AudioSource si no está asignado
        if (fuenteAudio == null)
        {
            fuenteAudio = gameObject.AddComponent<AudioSource>();
            fuenteAudio.playOnAwake = false;
            fuenteAudio.loop = false;
            fuenteAudio.spatialBlend = 0.5f; // semiespacial
        }
    }

    void Start()
    {
        if (modeloZona != null && materialInactivo != null)
            modeloZona.material = materialInactivo;
    }

    // ─────────────────────────────────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (_activada) return;
        if (!other.CompareTag("Player")) return;

        _activada = true;

        if (modeloZona != null && materialActivo != null)
            modeloZona.material = materialActivo;

        // ── Audio: sonido al pisar la zona ───────────────────────────────
        if (fuenteAudio != null && clipZona != null)
            fuenteAudio.PlayOneShot(clipZona, volumenZona);

        // ── ▼ LUZ: apagar al pisar (NUEVO) ───────────────────────────────
        if (luzZona != null)
        {
            if (apagarConFade)
                StartCoroutine(FadeOutLuzCO());
            else
                luzZona.enabled = false;
        }
        // ── ▲ LUZ ────────────────────────────────────────────────────────

        if (gestorZonas != null)
            gestorZonas.ZonaActivada();

        Debug.Log($"[Zona] {gameObject.name} activada.");
    }

    // ── ▼ CORRUTINA FADE (NUEVO) ─────────────────────────────────────────
    IEnumerator FadeOutLuzCO()
    {
        float intensidadInicial = luzZona.intensity;
        float tiempo = 0f;

        while (tiempo < duracionFade)
        {
            tiempo += Time.deltaTime;
            luzZona.intensity = Mathf.Lerp(intensidadInicial, 0f, tiempo / duracionFade);
            yield return null;
        }

        luzZona.enabled = false;
        luzZona.intensity = intensidadInicial; // restaurar por si se llama Reiniciar()
    }
    // ── ▲ CORRUTINA FADE ─────────────────────────────────────────────────

    // ─────────────────────────────────────────────────────────────────────
    public bool EstaActivada() => _activada;

    public void Reiniciar()
    {
        _activada = false;

        if (modeloZona != null && materialInactivo != null)
            modeloZona.material = materialInactivo;

        // ── ▼ LUZ: reencender al reiniciar (NUEVO) ───────────────────────
        if (luzZona != null)
            luzZona.enabled = true;
        // ── ▲ LUZ ────────────────────────────────────────────────────────
    }
}

