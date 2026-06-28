using UnityEngine;

/// <summary>
/// Hace que este GameObject siempre mire hacia la cámara principal.
/// Ponlo en el Canvas World Space del nombre del NPC.
///
/// SETUP:
///   NPC
///     └── NombreNPC          ← Canvas (World Space) + este script
///           └── Text / TMP   ← el texto del nombre
/// </summary>
public class MirarCamara : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main == null) return;
        transform.LookAt(
            transform.position + Camera.main.transform.rotation * Vector3.forward,
            Camera.main.transform.rotation * Vector3.up
        );
    }
}
