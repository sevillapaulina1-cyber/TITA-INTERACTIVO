using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Va en cada zona del suelo. Cuando el jugador la pisa se activa
/// y notifica al GestorZonas.
///
/// SETUP EN UNITY:
///   1. Crea un GameObject con un modelo visible (plataforma, alfombra, etc.)
///   2. Agrégale un Box Collider → marca "Is Trigger" ✓
///   3. Agrégale este script
///   4. Asigna gestorZonas → el GameObject con GestorZonas.cs
///
/// VISUAL FEEDBACK (opcional):
///   - Asigna materialInactivo y materialActivo para que cambie de color al pisarse
///   - Ej: inactivo = gris, activo = verde
/// </summary>
public class ZonaActivacion : MonoBehaviour
{
    [Header("── Gestor ───────────────────────────────")]
    public GestorZonas gestorZonas;

    [Header("── Visual feedback (opcional) ──────────")]
    public Renderer modeloZona;        // el Renderer de la plataforma/alfombra
    public Material materialInactivo;  // color por defecto
    public Material materialActivo;    // color al ser pisada

    // ── Estado ────────────────────────────────────────────────────────────
    bool _activada = false;

    // ─────────────────────────────────────────────────────────────────────
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

        // Cambiar visual
        if (modeloZona != null && materialActivo != null)
            modeloZona.material = materialActivo;

        // Notificar al gestor
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
