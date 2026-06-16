using UnityEngine;

/// <summary>
/// Va en cada zona del suelo. Cuando el jugador la pisa se activa
/// y notifica al GestorZonas.
/// MODIFICADO: Añade sonido al pisar la zona.
/// </summary>
public class ZonaActivacion : MonoBehaviour
{
    [Header("── Gestor ───────────────────────────────")]
    public GestorZonas gestorZonas;

    [Header("── Visual feedback (opcional) ──────────")]
    public Renderer modeloZona;
    public Material materialInactivo;
    public Material materialActivo;

    // ── ▼ AUDIO (NUEVO) ──────────────────────────────────────────────────
    [Header("── Audio ───────────────────────────────")]
    [Tooltip("AudioSource para el sonido de zona (se crea automáticamente si está vacío)")]
    public AudioSource fuenteAudio;
    [Tooltip("Clip que suena al pisar la zona")]
    public AudioClip clipZona;
    [Range(0f, 1f)]
    public float volumenZona = 0.85f;
    // ── ▲ AUDIO ──────────────────────────────────────────────────────────

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

        // ── ▼ AUDIO: sonido al pisar la zona (NUEVO) ─────────────────────
        if (fuenteAudio != null && clipZona != null)
            fuenteAudio.PlayOneShot(clipZona, volumenZona);
        // ── ▲ AUDIO ──────────────────────────────────────────────────────

        if (gestorZonas != null)
            gestorZonas.ZonaActivada();

        Debug.Log($"[Zona] {gameObject.name} activada.");
    }

    // ─────────────────────────────────────────────────────────────────────
    public bool EstaActivada() => _activada;

    public void Reiniciar()
    {
        _activada = false;
        if (modeloZona != null && materialInactivo != null)
            modeloZona.material = materialInactivo;
    }
}

