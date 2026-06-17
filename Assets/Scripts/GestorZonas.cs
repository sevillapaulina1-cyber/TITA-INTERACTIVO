using System.Collections;
using UnityEngine;

/// <summary>
/// Gestiona las 3 zonas del puzzle entre momento 4 y 5.
/// Cuando el jugador pisa las 3 zonas aparecen las monedas.
///
/// SETUP EN UNITY:
/// ─────────────────────────────────────────────────────
/// GestorZonas_4a5                ← GameObject vacío
///   └── GestorZonas.cs
///         ├── momentoQueActiva → 4
///         ├── zonas[]          → [Zona1, Zona2, Zona3]
///         ├── monedas[]        → [Moneda1, Moneda2, Moneda3]
///         ├── mensajeSiguientePaso → "Vuelve a hablar con el NPC"
///         └── forzarActivo     → marcar solo para debug
///
/// Zona1 / Zona2 / Zona3          ← GameObject con modelo visible
///   ├── Box Collider (Is Trigger ✓)
///   ├── MeshRenderer (la plataforma/alfombra)
///   └── ZonaActivacion.cs
///         └── gestorZonas → GestorZonas_4a5
///
/// En SistemaDialogo del momento 5:
///   puzzlePrevio → GestorZonas_4a5
/// ─────────────────────────────────────────────────────
/// </summary>
public class GestorZonas : MonoBehaviour
{
    [Header("── Identificación ──────────────────────")]
    public int momentoQueActiva = 4;

    [Header("── Misión ───────────────────────────────")]
    public string descripcionZonas = "Pisa las 3 zonas marcadas";
    public string descripcionMonedas = "Recoge las monedas";

    [Header("── Siguiente paso ───────────────────────")]
    [Tooltip("Mensaje que aparece al completar el puzzle")]
    public string mensajeSiguientePaso = "Vuelve a hablar con el NPC";

    [Header("── Zonas ───────────────────────────────")]
    public ZonaActivacion[] zonas;

    [Header("── Monedas ─────────────────────────────")]
    public GameObject[] monedas;
    public int totalMonedas = 3;

    [Header("── Efectos (opcional) ──────────────────")]
    public ParticleSystem efectoAparicion;
    public AudioClip sonidoMonedas;

    [Header("── Debug ────────────────────────────────")]
    [Tooltip("Marca para probar sin pasar por el momento 4")]
    public bool forzarActivo = false;

    // ── Estado ────────────────────────────────────────────────────────────
    int _zonasActivadas = 0;
    int _monedasRecogidas = 0;
    bool _activo = false;
    bool _monedasVisibles = false;
    bool _completado = false;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        ToggleMonedas(false);

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
        _monedasRecogidas = 0;

        if (UIObjetivo.Instance != null)
            UIObjetivo.Instance.MostrarObjetivo(
                descripcionZonas, 0, zonas != null ? zonas.Length : 3);

        Debug.Log("[GestorZonas] Puzzle iniciado — pisa las 3 zonas.");
    }

    // ─────────────────────────────────────────────────────────────────────
    public void ZonaActivada()
    {
        if (!_activo || _monedasVisibles) return;

        _zonasActivadas++;

        if (UIObjetivo.Instance != null)
            UIObjetivo.Instance.ActualizarProgreso(
                _zonasActivadas, zonas != null ? zonas.Length : 3);

        Debug.Log($"[GestorZonas] Zonas: {_zonasActivadas}/{zonas.Length}");

        if (_zonasActivadas >= zonas.Length)
            StartCoroutine(AparecerMonedasCO());
    }

    IEnumerator AparecerMonedasCO()
    {
        _monedasVisibles = true;
        yield return new WaitForSeconds(0.3f);

        if (efectoAparicion != null)
            foreach (var m in monedas)
                if (m != null)
                    Instantiate(efectoAparicion, m.transform.position, Quaternion.identity);

        if (sonidoMonedas != null)
            AudioSource.PlayClipAtPoint(sonidoMonedas, transform.position);

        ToggleMonedas(true);

        if (UIObjetivo.Instance != null)
            UIObjetivo.Instance.MostrarObjetivo(descripcionMonedas, 0, totalMonedas);

        Debug.Log("[GestorZonas] ¡Monedas aparecidas!");
    }

    // ─────────────────────────────────────────────────────────────────────
    public void MonedaRecogida()
    {
        if (!_monedasVisibles) return;
        _monedasRecogidas++;

        if (UIObjetivo.Instance != null)
            UIObjetivo.Instance.ActualizarProgreso(_monedasRecogidas, totalMonedas);

        if (_monedasRecogidas >= totalMonedas)
            Completado();
    }

    void Completado()
    {
        _completado = true;
        _activo = false;

        // ── MODIFICADO: mostrar "Vuelve a hablar con el NPC" en vez de fade out inmediato
        if (UIObjetivo.Instance != null)
            UIObjetivo.Instance.MostrarSiguientePaso(mensajeSiguientePaso);

        Debug.Log("[GestorZonas] ¡Completado! Momento 5 habilitado.");
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>SistemaDialogo del momento 5 consulta esto para bloquear.</summary>
    public bool PuzzlePendiente() => _activo && !_completado;

    void ToggleMonedas(bool activo)
    {
        if (monedas == null) return;
        foreach (var m in monedas)
            if (m != null) m.SetActive(activo);
    }
}
