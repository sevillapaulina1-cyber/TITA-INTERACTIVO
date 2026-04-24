using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gestiona la tarea de recolección de monedas entre momentos específicos.
/// Se activa automáticamente cuando el SistemaDialogo termina su conversación.
///
/// SETUP:
///   1. Crea un GameObject vacío "RecolectorMonedas_1a2" para la tarea entre momento 1 y 2.
///   2. Crea otro "RecolectorMonedas_4a5" para la tarea entre momento 4 y 5.
///   3. Asigna este script a cada uno y configura en el Inspector:
///      - momentoQueActiva   → el momento cuya conversación dispara esta tarea (1 o 4)
///      - totalMonedas       → 3
///      - monedas            → arrastra las 3 monedas de esa zona
///      - textoUI            → Text opcional para mostrar "Monedas: 0/3"
/// </summary>
public class RecolectorMonedas : MonoBehaviour
{
    [Header("── Identificación ──────────────────────")]
    [Tooltip("Después de qué momento se activa esta tarea (1 para 1→2, 4 para 4→5)")]
    public int momentoQueActiva = 1;

    [Header("── Monedas ─────────────────────────────")]
    public int totalMonedas = 3;
    public GameObject[] monedas;        // arrastra las 3 monedas de esta zona

    [Header("── UI (opcional) ───────────────────────")]
    [Tooltip("Text para mostrar 'Monedas: 0/3'. Déjalo en None si no quieres UI.")]
    public Text textoContador;

    // ── Estado interno ────────────────────────────────────────────────────
    int _recolectadas = 0;
    bool _tareaActiva = false;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        // Desactivar monedas al inicio — se activan cuando empieza la tarea
        ToggleMonedas(false);
        ActualizarUI();
    }

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (_tareaActiva) return;
        if (GameManager.Instance == null) return;

        // Se activa cuando el GameManager ya pasó el momento correspondiente
        if (GameManager.Instance.MomentoActual == momentoQueActiva)
            IniciarTarea();
    }

    // ─────────────────────────────────────────────────────────────────────
    void IniciarTarea()
    {
        _tareaActiva = true;
        _recolectadas = 0;
        ToggleMonedas(true);
        ActualizarUI();
        Debug.Log($"[RecolectorMonedas] Tarea iniciada: recolecta {totalMonedas} monedas.");
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Llamado por el script Moneda.cs cuando el jugador toca una moneda.
    /// </summary>
    public void MonedaRecolectada()
    {
        if (!_tareaActiva) return;

        _recolectadas++;
        ActualizarUI();
        Debug.Log($"[RecolectorMonedas] {_recolectadas}/{totalMonedas}");

        if (_recolectadas >= totalMonedas)
            TareaCompletada();
    }

    // ─────────────────────────────────────────────────────────────────────
    void TareaCompletada()
    {
        _tareaActiva = false;
        _recolectadas = totalMonedas; // marcar como completo
        ActualizarUI();
        Debug.Log($"[RecolectorMonedas] ¡Tarea completada! Momento {momentoQueActiva + 1} habilitado.");

        // Desactivar este script para que no vuelva a iniciarse
        this.enabled = false;
    }

    // ─────────────────────────────────────────────────────────────────────
    void ToggleMonedas(bool activo)
    {
        if (monedas == null) return;
        foreach (var m in monedas)
            if (m != null) m.SetActive(activo);
    }

    // ─────────────────────────────────────────────────────────────────────
    void ActualizarUI()
    {
        if (textoContador == null) return;
        textoContador.text = _tareaActiva
            ? $"Monedas: {_recolectadas}/{totalMonedas}"
            : (_recolectadas >= totalMonedas ? "¡Completado!" : "");
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Devuelve true si la tarea de este recolector está pendiente.
    /// SistemaDialogo la consulta para bloquear la interacción si aún hay monedas.
    /// </summary>
    public bool TareaPendiente()
    {
        // Pendiente = el momento ya pasó pero aún no se completó la recolecta
        if (GameManager.Instance == null) return false;
        return GameManager.Instance.MomentoActual == momentoQueActiva && _recolectadas < totalMonedas;
    }
}
