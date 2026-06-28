using System.Collections;
using UnityEngine;

/// <summary>
/// Va en cada zona del suelo. Cuando el jugador la pisa se activa
/// y notifica al GestorZonas.
/// MODIFICADO: Las zonas empiezan DESHABILITADAS. GestorZonas llama
/// Habilitar() al iniciar el puzzle para evitar activaciones prematuras.
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

    [Header("── Luz de zona ─────────────────────────")]
    [Tooltip("Light que ilumina la zona. Se apaga al pisarla.")]
    public Light luzZona;
    [Tooltip("Si está marcado, la luz se apaga gradualmente en vez de instantáneo")]
    public bool apagarConFade = false;
    [Tooltip("Duración del fade en segundos (solo si apagarConFade está activo)")]
    [Range(0.1f, 2f)]
    public float duracionFade = 0.5f;

    bool _activada = false;
    // ── ▼ NUEVO: la zona ignora triggers hasta que GestorZonas la habilite ──
    bool _habilitada = false;
    // ── ▲ NUEVO ──────────────────────────────────────────────────────────────

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (fuenteAudio == null)
        {
            fuenteAudio = gameObject.AddComponent<AudioSource>();
            fuenteAudio.playOnAwake = false;
            fuenteAudio.loop = false;
            fuenteAudio.spatialBlend = 0.5f;
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
        if (!_habilitada) return;   // ← NUEVO: bloqueado hasta que empiece el puzzle
        if (_activada) return;
        if (!other.CompareTag("Player")) return;

        _activada = true;

        if (modeloZona != null && materialActivo != null)
            modeloZona.material = materialActivo;

        if (fuenteAudio != null && clipZona != null)
            fuenteAudio.PlayOneShot(clipZona, volumenZona);

        if (luzZona != null)
        {
            if (apagarConFade)
                StartCoroutine(FadeOutLuzCO());
            else
                luzZona.enabled = false;
        }

        if (gestorZonas != null)
            gestorZonas.ZonaActivada();

        Debug.Log($"[Zona] {gameObject.name} activada.");
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Llamado por GestorZonas.IniciarPuzzle() para habilitar la detección.
    /// Hasta que se llame este método, OnTriggerEnter se ignora por completo.
    /// </summary>
    public void Habilitar()
    {
        _habilitada = true;
        Debug.Log($"[Zona] {gameObject.name} habilitada.");
    }

    // ─────────────────────────────────────────────────────────────────────
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
        luzZona.intensity = intensidadInicial;
    }

    // ─────────────────────────────────────────────────────────────────────
    public bool EstaActivada() => _activada;

    public void Reiniciar()
    {
        _activada = false;
        _habilitada = false;   // ← NUEVO: también reinicia el bloqueo

        if (modeloZona != null && materialInactivo != null)
            modeloZona.material = materialInactivo;

        if (luzZona != null)
            luzZona.enabled = true;
    }
}

