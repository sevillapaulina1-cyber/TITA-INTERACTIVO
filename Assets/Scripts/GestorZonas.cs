using System.Collections;
using UnityEngine;

/// <summary>
/// Gestiona las 3 zonas del puzzle entre momento 4 y 5.
/// Las zonas arrancan DESHABILITADAS; se habilitan solo cuando
/// IniciarPuzzle() es llamado (tras recibir la misión).
/// Al completar llama a UIObjetivo.MostrarPantallaCompletado().
/// </summary>
public class GestorZonas : MonoBehaviour
{
    [Header("── Identificación ──────────────────────")]
    public int momentoQueActiva = 4;

    [Header("── Misión ───────────────────────────────")]
    public string descripcionZonas = "Pisa las 3 zonas marcadas";

    [Header("── Zonas ───────────────────────────────")]
    public ZonaActivacion[] zonas;

    [Header("── Siguiente paso ───────────────────────")]
    [Tooltip("Mensaje persistente que aparece tras la pantalla de completado")]
    public string textoVolverANPC = "Vuelve a hablar con SamuVR";

    [Header("── Debug ────────────────────────────────")]
    [Tooltip("Marca para probar sin pasar por el momento 4")]
    public bool forzarActivo = false;

    int _zonasActivadas = 0;
    bool _activo = false;
    bool _completado = false;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (forzarActivo)
            IniciarPuzzle();
        else
            StartCoroutine(EsperarMomentoCO());
    }

    IEnumerator EsperarMomentoCO()
    {
        yield return new WaitUntil(() =>
            GameManager.Instance != null &&
            GameManager.Instance.MomentoActual == momentoQueActiva &&
            !_completado);
        IniciarPuzzle();
    }

    void IniciarPuzzle()
    {
        _activo = true;
        _zonasActivadas = 0;

        // ── ▼ NUEVO: habilitar zonas ahora que el puzzle ha comenzado ──────
        if (zonas != null)
        {
            foreach (var zona in zonas)
            {
                if (zona != null)
                {
                    zona.Reiniciar();   // limpia estado por si había sido pisada antes
                    zona.Habilitar();   // permite detectar al jugador
                }
            }
        }
        // ── ▲ NUEVO ──────────────────────────────────────────────────────────

        if (UIObjetivo.Instance != null)
            UIObjetivo.Instance.MostrarObjetivo(
                descripcionZonas, 0, zonas != null ? zonas.Length : 3);

        Debug.Log("[GestorZonas] Puzzle iniciado — pisa las 3 zonas.");
    }

    // ─────────────────────────────────────────────────────────────────────
    public void ZonaActivada()
    {
        if (!_activo || _completado) return;

        _zonasActivadas++;

        if (UIObjetivo.Instance != null)
            UIObjetivo.Instance.ActualizarProgreso(
                _zonasActivadas, zonas != null ? zonas.Length : 3);

        Debug.Log($"[GestorZonas] Zonas: {_zonasActivadas}/{zonas.Length}");

        if (_zonasActivadas >= zonas.Length)
            Completado();
    }

    // ─────────────────────────────────────────────────────────────────────
    void Completado()
    {
        _completado = true;
        _activo = false;

        if (UIObjetivo.Instance != null)
            UIObjetivo.Instance.MostrarPantallaCompletado(textoVolverANPC);

        Debug.Log("[GestorZonas] ¡Completado! Momento 5 habilitado.");
    }

    // ─────────────────────────────────────────────────────────────────────
    public bool PuzzlePendiente() => !_completado;
}
