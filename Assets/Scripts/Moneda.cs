using UnityEngine;

/// <summary>
/// Script que va en cada moneda del mundo.
/// Puede notificar a RecolectorMonedas (tramos normales)
/// o a PuzzlePalancas (tramo 4→5 con palancas).
/// Solo uno de los dos debe estar asignado por moneda.
///
/// SETUP:
///   Monedas del tramo 1→2:  asignar recolector, dejar gestorPalancas vacío
///   Monedas del tramo 4→5:  asignar gestorPalancas, dejar recolector vacío
///
/// INSPECTOR:
///   recolector       → RecolectorMonedas de esta zona  (tramos normales)
///   gestorPalancas   → PuzzlePalancas de esta zona     (tramo 4→5)
///   girar            → true
///   velocidadGiro    → 90
///   sonidoRecolecta  → AudioClip opcional
/// </summary>
public class Moneda : MonoBehaviour
{
    [Header("── Recolector (tramos normales) ────────")]
    [Tooltip("Asigna esto para los tramos sin palancas")]
    public RecolectorMonedas recolector;

    [Header("── Puzzle palancas (tramo 4→5) ─────────")]
    [Tooltip("Asigna esto para el tramo con palancas")]
    public PuzzlePalancas gestorPalancas;

    [Header("── Animación ───────────────────────────")]
    public bool girar = true;
    public float velocidadGiro = 90f;

    [Header("── Audio (opcional) ───────────────────")]
    public AudioClip sonidoRecolecta;

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (girar)
            transform.Rotate(Vector3.up, velocidadGiro * Time.deltaTime);
    }

    // ─────────────────────────────────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (sonidoRecolecta != null)
            AudioSource.PlayClipAtPoint(sonidoRecolecta, transform.position);

        // Notificar al gestor correspondiente
        if (gestorPalancas != null)
            gestorPalancas.MonedaRecogida();
        else if (recolector != null)
            recolector.MonedaRecolectada();

        gameObject.SetActive(false);
    }
}
