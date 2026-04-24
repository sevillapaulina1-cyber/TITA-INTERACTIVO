using UnityEngine;

/// <summary>
/// Script que va en cada moneda del mundo.
///
/// SETUP:
///   1. Crea un GameObject con una malla (cilindro, esfera, etc.) y un Collider.
///   2. Marca el Collider como "Is Trigger" ✓
///   3. Agrega este script y asigna el RecolectorMonedas correspondiente.
///   4. Duplica x3 para tener las 3 monedas de cada zona.
///
/// OPCIONAL:
///   - Asigna un AudioClip en "sonidoRecolecta" para reproducir al tocar.
///   - Activa "girar" para que la moneda rote sobre su eje.
/// </summary>
public class Moneda : MonoBehaviour
{
    [Header("── Recolector ───────────────────────────")]
    [Tooltip("Arrastra aquí el RecolectorMonedas de esta zona")]
    public RecolectorMonedas recolector;

    [Header("── Animación ───────────────────────────")]
    public bool  girar          = true;
    public float velocidadGiro  = 90f;   // grados por segundo

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
        if (recolector == null) return;

        // Reproducir sonido si hay uno asignado
        if (sonidoRecolecta != null)
            AudioSource.PlayClipAtPoint(sonidoRecolecta, transform.position);

        // Notificar al recolector
        recolector.MonedaRecolectada();

        // Desactivar la moneda
        gameObject.SetActive(false);
    }
}
