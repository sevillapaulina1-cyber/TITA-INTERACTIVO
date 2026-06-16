using UnityEngine;

/// <summary>
/// Coloca este script en la EscenaPrincipal (en cualquier GameObject de la escena).
/// Su único trabajo es decirle al AudioManager que empiece la música del juego
/// cuando se carga esta escena.
///
/// SETUP EN UNITY:
///   EscenaPrincipal → GameObject vacío "InicializadorAudio"
///     └── InicializadorAudio.cs
///
/// No necesita ningún campo asignado en el Inspector.
/// </summary>
public class InicializadorAudio : MonoBehaviour
{
    void Start()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.IniciarMusicaJuego();
        else
            Debug.LogWarning("[InicializadorAudio] AudioManager no encontrado. ¿Está en la escena MenuInicio?");
    }
}
