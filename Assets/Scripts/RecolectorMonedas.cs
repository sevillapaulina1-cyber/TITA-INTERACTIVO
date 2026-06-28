using UnityEngine;

/// <summary>
/// Gestiona la tarea de recolección de monedas entre momentos.
/// Se activa automáticamente tras el momento indicado.
/// Al completar llama a UIObjetivo.MostrarPantallaCompletado() que:
///   1. Muestra imagen central de completado 4 segundos
///   2. Luego muestra el objetivo persistente "Vuelve a hablar con el NPC"
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
    [Tooltip("Mensaje persistente que aparece tras la pantalla de completado")]
    public string mensajeSiguientePaso = "Vuelve a hablar con el NPC";

    [Header("── Monedas ─────────────────────────────")]
    public int totalMonedas = 3;
    public GameObject[] monedas;

    int _recolectadas = 0;
    bool _tareaActiva = false;
    bool _completado = false;

    // ─────────────────────────────────────────────────────────────────────
    void Start() => ToggleMonedas(false);

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

        if (UIObjetivo.Instance != null)
            UIObjetivo.Instance.MostrarObjetivo(descripcionMision, 0, totalMonedas);

        Debug.Log($"[RecolectorMonedas] Iniciado — {descripcionMision} ({totalMonedas})");
    }

    // ─────────────────────────────────────────────────────────────────────
    public void MonedaRecolectada()
    {
        if (!_tareaActiva) return;

        _recolectadas++;
        Debug.Log($"[RecolectorMonedas] {_recolectadas}/{totalMonedas}");

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

        // Pantalla central de completado → luego objetivo persistente de volver al NPC
        if (UIObjetivo.Instance != null)
            UIObjetivo.Instance.MostrarPantallaCompletado(mensajeSiguientePaso);

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
    public bool TareaPendiente()
    {
        if (GameManager.Instance == null) return false;
        return GameManager.Instance.MomentoActual == momentoQueActiva && !_completado;
    }
}
