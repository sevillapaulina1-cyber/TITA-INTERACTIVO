using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Va en cada palanca del mundo. El jugador se acerca y presiona E para bajarla.
/// Notifica al PuzzlePalancas cuando se activa.
///
/// SETUP EN UNITY — jerarquía de cada palanca:
/// ─────────────────────────────────────────────
/// Palanca_01                      ← GameObject raíz
///   ├── Collider (Box o Capsule)  ← NO es trigger, es para el raycast
///   ├── MeshRenderer              ← modelo visual de la palanca
///   ├── BrazoPalanca              ← hijo que rota al bajar (pivot en la base)
///   │     └── MeshRenderer        ← la barra/brazo que se inclina
///   └── Palanca.cs                ← este script
///         ├── gestorPuzzle    → PuzzlePalancas (GameObject con ese script)
///         ├── brazoPalanca    → BrazoPalanca (el hijo que rota)
///         ├── anguloActivo    → -60  (cuánto rota al bajar, en grados X)
///         ├── duracionAnimacion → 0.4
///         └── textoInteraccion → Text de UI "Presiona E"
///
/// INSPECTOR:
///   gestorPuzzle       → arrastra el GameObject con PuzzlePalancas
///   brazoPalanca       → el Transform hijo que rota visualmente
///   anguloActivo       → -60 (negativo = inclina hacia adelante)
///   duracionAnimacion  → 0.4
///   distanciaActivar   → 3
///   textoInteraccion   → Text de UI compartido (el mismo de los diálogos)
/// </summary>
public class Palanca : MonoBehaviour
{
    [Header("── Gestor ───────────────────────────────")]
    public PuzzlePalancas gestorPuzzle;

    [Header("── Animación ───────────────────────────")]
    public Transform brazoPalanca;          // hijo que rota al activar
    public float     anguloActivo      = -60f;   // rotación X al bajar
    public float     duracionAnimacion =   0.4f;

    [Header("── Interacción ─────────────────────────")]
    public Transform jugador;
    public float     distanciaActivar  =   3f;
    public Text      textoInteraccion;          // el mismo Text "Presiona E" del HUD

    [Header("── Audio (opcional) ───────────────────")]
    public AudioClip sonidoPalanca;

    // ── Estado ────────────────────────────────────────────────────────────
    bool _activada  = false;
    bool _animando  = false;
    Quaternion _rotacionInicial;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (brazoPalanca != null)
            _rotacionInicial = brazoPalanca.localRotation;
    }

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (_activada || _animando) return;
        if (jugador == null) return;

        float distancia = Vector3.Distance(jugador.position, transform.position);

        if (distancia <= distanciaActivar)
        {
            if (textoInteraccion != null)
                textoInteraccion.text = "Presiona E para bajar la palanca";

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                StartCoroutine(ActivarCO());
        }
        else
        {
            if (textoInteraccion != null)
                textoInteraccion.text = "";
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator ActivarCO()
    {
        _animando = true;

        if (textoInteraccion != null)
            textoInteraccion.text = "";

        // Sonido
        if (sonidoPalanca != null)
            AudioSource.PlayClipAtPoint(sonidoPalanca, transform.position);

        // Animar la rotación del brazo
        if (brazoPalanca != null)
        {
            Quaternion rotDestino = Quaternion.Euler(anguloActivo, 0f, 0f);
            float t = 0f;
            while (t < duracionAnimacion)
            {
                t += Time.deltaTime;
                brazoPalanca.localRotation = Quaternion.Lerp(
                    _rotacionInicial, rotDestino, t / duracionAnimacion
                );
                yield return null;
            }
            brazoPalanca.localRotation = rotDestino;
        }
        else
        {
            yield return new WaitForSeconds(duracionAnimacion);
        }

        _activada = true;
        _animando = false;

        // Notificar al gestor
        if (gestorPuzzle != null)
            gestorPuzzle.PalancaActivada();
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>Devuelve si la palanca ya fue bajada.</summary>
    public bool EstaActivada() => _activada;

    /// <summary>Reinicia la palanca a su estado inicial (útil si se reinicia el juego).</summary>
    public void Reiniciar()
    {
        _activada = false;
        _animando = false;
        if (brazoPalanca != null)
            brazoPalanca.localRotation = _rotacionInicial;
    }
}
