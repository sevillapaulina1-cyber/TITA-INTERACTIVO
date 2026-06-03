using UnityEngine;

public class Moneda : MonoBehaviour
{
    [Header("── Recolector (tramos normales) ────────")]
    [Tooltip("Asigna para tramos sin puzzle de zonas")]
    public RecolectorMonedas recolector;

    [Header("── Gestor Zonas (tramo 4→5) ─────────────")]
    [Tooltip("Asigna GestorZonas_4a5 para las monedas del puzzle de zonas")]
    public GestorZonas gestorZonas;

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
        if (gestorZonas != null)
            gestorZonas.MonedaRecogida();
        else if (recolector != null)
            recolector.MonedaRecolectada();

        gameObject.SetActive(false);
    }
}
