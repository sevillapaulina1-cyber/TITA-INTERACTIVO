using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class Palanca : MonoBehaviour
{
    [Header("── Gestor ───────────────────────────────")]
    public PuzzlePalancas gestorPuzzle;

    [Header("── Animación ───────────────────────────")]
    public Transform brazoPalanca;
    public float anguloActivo = -60f;
    public float duracionAnimacion = 0.4f;

    [Header("── Interacción ─────────────────────────")]
    public Transform jugador;
    public float distanciaActivar = 4f;
    public Text textoInteraccion;

    [Header("── Audio (opcional) ───────────────────")]
    public AudioClip sonidoPalanca;

    bool _activada = false;
    bool _animando = false;
    Quaternion _rotacionInicial;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (brazoPalanca != null)
            _rotacionInicial = brazoPalanca.localRotation;

        if (jugador == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) jugador = p.transform;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (_activada || _animando) return;
        if (jugador == null) { Debug.Log("[Palanca] jugador null"); return; }
        if (gestorPuzzle == null || !gestorPuzzle.PuzzlePendiente())
        { Debug.Log($"[Palanca] puzzle no pendiente"); return; }

        Ray ray = new Ray(jugador.position, jugador.forward);
        Debug.DrawRay(jugador.position, jugador.forward * distanciaActivar, Color.red);

        if (Physics.Raycast(ray, out RaycastHit hit, distanciaActivar))
            Debug.Log($"[Palanca] Raycast golpeó: {hit.collider.gameObject.name}");
        else
            Debug.Log("[Palanca] Raycast no golpeó nada");
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator ActivarCO()
    {
        _animando = true;

        if (textoInteraccion != null)
            textoInteraccion.text = "";

        if (sonidoPalanca != null)
            AudioSource.PlayClipAtPoint(sonidoPalanca, transform.position);

        if (brazoPalanca != null)
        {
            Quaternion rotDestino = Quaternion.Euler(anguloActivo, 0f, 0f);
            float t = 0f;
            while (t < duracionAnimacion)
            {
                t += Time.deltaTime;
                brazoPalanca.localRotation = Quaternion.Lerp(
                    _rotacionInicial, rotDestino, t / duracionAnimacion);
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

        if (gestorPuzzle != null)
            gestorPuzzle.PalancaActivada();
    }

    // ─────────────────────────────────────────────────────────────────────
    public bool EstaActivada() => _activada;

    public void Reiniciar()
    {
        _activada = false;
        _animando = false;
        if (brazoPalanca != null)
            brazoPalanca.localRotation = _rotacionInicial;
    }
}

