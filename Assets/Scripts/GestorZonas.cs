using System.Collections;
using UnityEngine;

/// <summary>
/// Gestiona las 3 zonas del puzzle entre momento 4 y 5.
/// Cuando el jugador pisa las 3 zonas el puzzle se completa directamente
/// (sin mecánica de monedas). Aparece "¡Completado!" y luego el aviso
/// persistente "Vuelve a hablar con SamuVR" que no se quita hasta que
/// el jugador presione E para hablar con el NPC.
///
/// SETUP EN UNITY:
/// ─────────────────────────────────────────────────────
/// GestorZonas_4a5                ← GameObject vacío
///   └── GestorZonas.cs
///         ├── momentoQueActiva → 4
///         ├── zonas[]          → [Zona1, Zona2, Zona3]
///         └── forzarActivo     → marcar solo para debug
///
/// Zona1 / Zona2 / Zona3          ← GameObject con modelo visible
///   ├── Box Collider (Is Trigger ✓)
///   ├── MeshRenderer (la plataforma/alfombra)
///   └── ZonaActivacion.cs
///         └── gestorZonas → GestorZonas_4a5
///
/// En SistemaDialogo del momento 5:
///   gestorZonasPrevio → GestorZonas_4a5
/// ─────────────────────────────────────────────────────
/// </summary>
public class GestorZonas : MonoBehaviour
{
    [Header("── Identificación ──────────────────────")]
    public int momentoQueActiva = 4;

    [Header("── Misión ───────────────────────────────")]
    public string descripcionZonas = "Pisa las 3 zonas marcadas";

    [Header("── Zonas ───────────────────────────────")]
    public ZonaActivacion[] zonas;

    [Header("── Debug ────────────────────────────────")]
    [Tooltip("Marca para probar sin pasar por el momento 4")]
    public bool forzarActivo = false;

    [Header("── Aviso al completar ──────────────────")]
    [Tooltip("Mensaje persistente tras completar las zonas, hasta que el jugador hable con el NPC")]
    public string textoVolverANPC = "Vuelve a hablar con SamuVR";

    // ── Estado ────────────────────────────────────────────────────────────
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

        // Muestra "¡Completado!" brevemente y luego deja fijo el aviso
        // persistente hasta que el jugador hable con el NPC.
        StartCoroutine(AvisarVolverNPCCO());

        Debug.Log("[GestorZonas] ¡Completado! Momento 5 habilitado.");
    }

    IEnumerator AvisarVolverNPCCO()
    {
        UIObjetivo.Instance.CompletarObjetivo();
        // Espera que termine la animación de "¡Completado!" antes de mostrar
        // el aviso persistente, para no pisar el fade out.
        yield return new WaitForSeconds(
            UIObjetivo.Instance.delayAlCompletar + UIObjetivo.Instance.duracionFade + 0.1f);
        UIObjetivo.Instance.MostrarObjetivoPersistente(textoVolverANPC);
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// SistemaDialogo del momento 5 consulta esto para bloquear el diálogo.
    /// Devuelve true mientras el puzzle no haya sido completado.
    /// Se vuelve false solo cuando el jugador ha pisado todas las zonas.
    /// </summary>
    public bool PuzzlePendiente() => !_completado;
}
