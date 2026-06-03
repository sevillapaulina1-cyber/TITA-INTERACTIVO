

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
            else Debug.LogWarning("[Palanca] No se encontró objeto con tag 'Player'.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (_activada || _animando) return;
        if (jugador == null) return;
        if (gestorPuzzle == null || !gestorPuzzle.PuzzlePendiente()) return;

        float distancia = Vector3.Distance(jugador.position, transform.position);

        if (distancia <= distanciaActivar)
        {
            Vector3 dirPalanca = (transform.position - jugador.position).normalized;
            float angulo = Vector3.Angle(jugador.forward, dirPalanca);

            if (angulo < 50f)
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