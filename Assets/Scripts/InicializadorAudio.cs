using UnityEngine;

/// <summary>
/// Coloca este script en la EscenaPrincipal.
/// Arranca la música de juego cuando la escena carga.
///
/// SETUP: GameObject vacío "InicializadorAudio" en EscenaPrincipal.
/// No necesita campos en el Inspector.
/// </summary>
public class InicializadorAudio : MonoBehaviour
{
    void Start()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.IniciarMusicaJuego();
        else
            Debug.LogWarning("[InicializadorAudio] AudioManager no encontrado.");
    }
}