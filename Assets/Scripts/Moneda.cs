using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class Moneda : MonoBehaviour
{
    [Header("── Recolector (tramos normales) ────────")]
    [Tooltip("Asigna para tramos sin puzzle de zonas")]
    public RecolectorMonedas recolector;

    // gestorZonas eliminado — GestorZonas ya no usa monedas (puzzle de zonas completa directo)

    [Header("── Animación ───────────────────────────")]
    public bool girar = true;
    public float velocidadGiro = 90f;

    [Header("── Audio ───────────────────────────────")]
    public AudioClip sonidoRecolecta;
    [Tooltip("Arrastra MixerPrincipal para que el slider de volumen lo afecte")]
    public AudioMixerGroup mixerGroup;   // ← asigna Master del MixerPrincipal

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

        // Reproduce el sonido en un GameObject temporal independiente
        // para que no se corte al desactivar esta moneda.
        if (sonidoRecolecta != null)
            PlaySonidoIndependiente(sonidoRecolecta, transform.position, mixerGroup);

        // Notificar al recolector (solo para tramos normales sin puzzle de zonas)
        if (recolector != null)
            recolector.MonedaRecolectada();

        gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Crea un AudioSource temporal en la escena que se destruye solo
    // al terminar el clip. Pasa por el Mixer para respetar el slider.
    static void PlaySonidoIndependiente(AudioClip clip, Vector3 posicion, AudioMixerGroup grupo)
    {
        GameObject go = new GameObject("SFX_Moneda");
        go.transform.position = posicion;

        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.outputAudioMixerGroup = grupo;   // conecta al Mixer
        src.spatialBlend = 0f;          // 2D (suena igual desde cualquier distancia)
        src.Play();

        Object.Destroy(go, clip.length + 0.1f);
    }
}