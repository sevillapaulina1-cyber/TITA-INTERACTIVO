using UnityEngine;

/// <summary>
/// Coloca este script en la EscenaPrincipal.
///
/// NOTA: La música del Día 1 la arranca TransicionDia al terminar su fade de entrada.
/// Este script ya NO llama IniciarMusicaJuego() para evitar que suene durante
/// la pantalla negra del intro del Día 1.
///
/// Si entras a la escena desde debug (debugIniciarDesdeMomento > 0 en GameManager),
/// la música sí arranca aquí directamente porque no hay intro de Día 1.
/// </summary>
public class InicializadorAudio : MonoBehaviour
{
    void Start()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[InicializadorAudio] AudioManager no encontrado.");
            return;
        }

        // Solo arrancar música aquí si estamos en modo debug (sin intro de Día 1)
        bool esDebug = GameManager.Instance != null && GameManager.Instance.DiaActual > 1;
        if (esDebug)
            AudioManager.Instance.IniciarMusicaJuego();

        // En el flujo normal, TransicionDia.IntroDia1CO() arranca la música
        // al terminar su fade de entrada al Día 1.
    }
}