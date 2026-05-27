using System.Collections;
using UnityEngine;

/// <summary>
/// Gestor del puzzle de palancas entre el momento 4 y el momento 5.
/// Espera a que las 3 palancas sean activadas (en cualquier orden)
/// y entonces hace aparecer todas las monedas a la vez.
/// Cuando se recogen todas las monedas, habilita al NPC del momento 5.
///
/// FLUJO:
///   Momento 4 termina → RecolectorMonedas notifica → PuzzlePalancas se activa
///   Jugador baja las 3 palancas (orden libre)
///   → Aparecen las 3 monedas + mensaje "¡Recoge las monedas!"
///   Jugador recoge las 3 monedas
///   → Momento 5 habilitado (SistemaDialogo responde al raycast normalmente)
///
/// SETUP EN UNITY — jerarquía:
/// ─────────────────────────────────────────────────────
/// PuzzlePalancas_4a5              ← GameObject vacío
///   └── PuzzlePalancas.cs         ← este script
///         ├── momentoQueActiva  → 4
///         ├── palancas[]        → [Palanca_01, Palanca_02, Palanca_03]
///         ├── monedas[]         → [Moneda_01, Moneda_02, Moneda_03]
///         ├── textoUI           → Text "Baja las palancas: 0/3"
///         └── efectoAparicion   → ParticleSystem opcional al aparecer monedas
///
/// IMPORTANTE:
///   - Las monedas deben tener el script Moneda.cs con su recolector asignado
///   - Las palancas deben tener Palanca.cs con gestorPuzzle → este GameObject
///   - Este script reemplaza RecolectorMonedas para el tramo 4→5
///     (RecolectorMonedas sigue funcionando para otros tramos)
///
/// INSPECTOR:
///   momentoQueActiva   → 4
///   palancas           → las 3 Palancas de la escena
///   monedas            → los 3 GameObjects de monedas (desactivados al inicio)
///   textoUI            → Text de HUD (puede ser null)
///   efectoAparicion    → ParticleSystem de partículas al aparecer (puede ser null)
///   totalMonedas       → 3
/// </summary>
public class PuzzlePalancas : MonoBehaviour
{
    [Header("── Identificación ──────────────────────")]
    [Tooltip("Después de qué momento se activa este puzzle (4 para el tramo 4→5)")]
    public int momentoQueActiva = 4;

    [Header("── Misión ───────────────────────────────")]
    public string descripcionPalancas = "Baja las palancas";
    public string descripcionMonedas = "Recoge las monedas";

    [Header("── Palancas ────────────────────────────")]
    public Palanca[] palancas;              // las 3 palancas de la zona

    [Header("── Monedas ─────────────────────────────")]
    public GameObject[] monedas;           // desactivadas al inicio
    public int totalMonedas = 3;

    [Header("── Efecto visual (opcional) ────────────")]
    [Tooltip("ParticleSystem que se reproduce al aparecer las monedas")]
    public ParticleSystem efectoAparicion;

    [Header("── Audio (opcional) ───────────────────")]
    public AudioClip sonidoMonedas;        // sonido al aparecer las monedas

    // ── Estado interno ────────────────────────────────────────────────────
    int _palancasActivadas = 0;
    int _monedasRecogidas = 0;
    bool _puzzleActivo = false;
    bool _monedasVisibles = false;
    bool _completado = false;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        ToggleMonedas(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (_puzzleActivo || _completado) return;
        if (GameManager.Instance == null) return;

        // Se activa cuando el momento 4 ya fue registrado
        if (GameManager.Instance.MomentoActual == momentoQueActiva)
            IniciarPuzzle();
    }

    // ─────────────────────────────────────────────────────────────────────
    void IniciarPuzzle()
    {
        _puzzleActivo = true;
        _palancasActivadas = 0;
        _monedasRecogidas = 0;

        // Mostrar misión: palancas
        if (UIObjetivo.Instance != null)
            UIObjetivo.Instance.MostrarObjetivo(
                descripcionPalancas, 0, palancas != null ? palancas.Length : 3);

        Debug.Log("[PuzzlePalancas] Puzzle iniciado — baja las 3 palancas.");
    }

    // ─────────────────────────────────────────────────────────────────────
    public void PalancaActivada()
    {
        if (!_puzzleActivo || _monedasVisibles) return;

        _palancasActivadas++;

        if (UIObjetivo.Instance != null)
            UIObjetivo.Instance.ActualizarProgreso(
                _palancasActivadas, palancas != null ? palancas.Length : 3);

        Debug.Log($"[PuzzlePalancas] Palancas: {_palancasActivadas}/{palancas.Length}");

        if (_palancasActivadas >= palancas.Length)
            StartCoroutine(AparecerMonedasCO());
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator AparecerMonedasCO()
    {
        _monedasVisibles = true;

        yield return new WaitForSeconds(0.3f);

        if (efectoAparicion != null)
            foreach (var moneda in monedas)
                if (moneda != null)
                    Instantiate(efectoAparicion, moneda.transform.position, Quaternion.identity);

        if (sonidoMonedas != null)
            AudioSource.PlayClipAtPoint(sonidoMonedas, transform.position);

        ToggleMonedas(true);

        // Cambiar misión: ahora recoger monedas
        if (UIObjetivo.Instance != null)
            UIObjetivo.Instance.MostrarObjetivo(descripcionMonedas, 0, totalMonedas);

        Debug.Log("[PuzzlePalancas] ¡Monedas aparecidas! Recógelas.");
    }

    // ─────────────────────────────────────────────────────────────────────
    public void MonedaRecogida()
    {
        if (!_monedasVisibles) return;

        _monedasRecogidas++;

        if (UIObjetivo.Instance != null)
            UIObjetivo.Instance.ActualizarProgreso(_monedasRecogidas, totalMonedas);

        Debug.Log($"[PuzzlePalancas] Monedas recogidas: {_monedasRecogidas}/{totalMonedas}");

        if (_monedasRecogidas >= totalMonedas)
            PuzzleCompletado();
    }

    // ─────────────────────────────────────────────────────────────────────
    void PuzzleCompletado()
    {
        _completado = true;
        _puzzleActivo = false;

        if (UIObjetivo.Instance != null)
            UIObjetivo.Instance.CompletarObjetivo();

        Debug.Log("[PuzzlePalancas] ¡Puzzle completado! Momento 5 habilitado.");
    }

    // ─────────────────────────────────────────────────────────────────────
    public bool PuzzlePendiente()
    {
        if (GameManager.Instance == null) return false;
        return GameManager.Instance.MomentoActual == momentoQueActiva && !_completado;
    }

    // ─────────────────────────────────────────────────────────────────────
    void ToggleMonedas(bool activo)
    {
        if (monedas == null) return;
        foreach (var m in monedas)
            if (m != null) m.SetActive(activo);
    }
}