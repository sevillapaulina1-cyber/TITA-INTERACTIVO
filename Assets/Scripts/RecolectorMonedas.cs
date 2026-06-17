using UnityEngine;

/// <summary>
/// Gestiona la tarea de recolección de monedas entre momentos.
/// Se activa automáticamente tras el momento indicado.
/// Usa UIObjetivo para mostrar la misión en pantalla.
///
/// SETUP EN UNITY:
///   GameObject vacío "RecolectorMonedas_1a2" (o el tramo que sea)
///     └── RecolectorMonedas.cs
///           ├── momentoQueActiva  → 1  (el momento tras el que se activa)
///           ├── descripcionMision → "Recoge las monedas"
///           ├── totalMonedas      → 3
///           └── monedas[]         → las 3 monedas de esa zona (desactivadas al inicio)
///
/// INSPECTOR:
///   momentoQueActiva   → número del momento tras el que se activa (ej. 1 para tramo 1→2)
///   descripcionMision  → texto que aparece en el panel de objetivo
///   totalMonedas       → 3
///   monedas[]          → GameObjects de las monedas
///   mensajeSiguientePaso → texto que aparece al completar (ej. "Vuelve a hablar con el NPC")
/// </summary>
public class RecolectorMonedas : MonoBehaviour
{
    [Header("── Identificación ──────────────────────")]
    [Tooltip("Después de qué momento se activa (ej. 1 para tramo 1→2)")]
    public int momentoQueActiva = 1;

    [Header("── Misión ───────────────────────────────")]
    [Tooltip("Texto que aparece en el panel de objetivo")]
    public string descripcionMision = "Recoge las monedas";

    [Header("── Siguiente paso ───────────────────────")]
    [Tooltip("Mensaje que aparece al completar el puzzle")]
    public string mensajeSiguientePaso = "Vuelve a hablar con el NPC";

    [Header("── Monedas ─────────────────────────────")]
    public int totalMonedas = 3;
    public GameObject[] monedas;

    // ── Estado interno ────────────────────────────────────────────────────
    int _recolectadas = 0;
    bool _tareaActiva = false;
    bool _completado = false;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        ToggleMonedas(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (_tareaActiva || _completado) return;
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.MomentoActual == momentoQueActiva)
            IniciarTarea();
    }

    // ─────────────────────────────────────────────────────────────────────
    void IniciarTarea()
    {
        _tareaActiva = true;
        _recolectadas = 0;

        ToggleMonedas(true);

        // Mostrar panel de objetivo
        if (UIObjetivo.Instance != null)
            UIObjetivo.Instance.MostrarObjetivo(descripcionMision, 0, totalMonedas);

        Debug.Log($"[RecolectorMonedas] Iniciado — {descripcionMision} ({totalMonedas})");
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>Llamado por Moneda.cs al recoger una moneda.</summary>
    public void MonedaRecolectada()
    {
        if (!_tareaActiva) return;

        _recolectadas++;
        Debug.Log($"[RecolectorMonedas] {_recolectadas}/{totalMonedas}");

        // Actualizar panel
        if (UIObjetivo.Instance != null)
            UIObjetivo.Instance.ActualizarProgreso(_recolectadas, totalMonedas);

        if (_recolectadas >= totalMonedas)
            TareaCompletada();
    }

    // ─────────────────────────────────────────────────────────────────────
    void TareaCompletada()
    {
        _tareaActiva = false;
        _completado = true;

        // ── MODIFICADO: mostrar "Vuelve a hablar con el NPC" en vez de fade out inmediato
        if (UIObjetivo.Instance != null)
            UIObjetivo.Instance.MostrarSiguientePaso(mensajeSiguientePaso);

        Debug.Log($"[RecolectorMonedas] ¡Completado! Momento {momentoQueActiva + 1} habilitado.");
    }

    // ─────────────────────────────────────────────────────────────────────
    void ToggleMonedas(bool activo)
    {
        if (monedas == null) return;
        foreach (var m in monedas)
            if (m != null) m.SetActive(activo);
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>SistemaDialogo consulta esto para bloquear la interacción.</summary>
    public bool TareaPendiente()
    {
        if (GameManager.Instance == null) return false;
        return GameManager.Instance.MomentoActual == momentoQueActiva && !_completado;
    }
}
