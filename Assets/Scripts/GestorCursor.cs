using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Punto ÚNICO de control del cursor para todo el proyecto.
///
/// PROBLEMA que resuelve: antes, SistemaDialogo, DialogoCelular y MenuPausa
/// tocaban Cursor.lockState / Cursor.visible cada uno por su cuenta. Si se
/// abría un diálogo y encima se abría el menú de pausa, al cerrar la pausa
/// el cursor se bloqueaba aunque el diálogo siguiera abierto debajo — el
/// mouse "desaparecía" y no se podía seleccionar nada.
///
/// SOLUCIÓN: ningún script toca Cursor.* directamente nunca más. En cambio:
///   - Al abrir un diálogo / celular / menú de pausa → GestorCursor.PedirLibre(this)
///   - Al cerrarlo                                    → GestorCursor.Liberar(this)
///
/// El cursor se mantiene SIEMPRE libre mientras haya al menos un sistema
/// pidiéndolo, sin importar el orden en que se abran/cierren los paneles,
/// ni si la ventana pierde y recupera el foco (clic afuera, alt-tab, etc.).
///
/// No requiere configuración: se crea solo la primera vez que se usa.
/// </summary>
public class GestorCursor : MonoBehaviour
{
    static readonly HashSet<object> _demandantes = new HashSet<object>();
    static GestorCursor _instancia;

    // ─────────────────────────────────────────────────────────────────────
    static void AsegurarInstancia()
    {
        if (_instancia != null) return;

        GameObject go = new GameObject("GestorCursor (auto)");
        _instancia = go.AddComponent<GestorCursor>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        if (_instancia != null && _instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        _instancia = this;
        DontDestroyOnLoad(gameObject);
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Pide que el cursor quede libre (visible y desbloqueado) mientras
    /// "quien" lo necesite. Pasar 'this' desde el script que llama.
    /// Ej: GestorCursor.PedirLibre(this);
    /// </summary>
    public static void PedirLibre(object quien)
    {
        AsegurarInstancia();
        _demandantes.Add(quien);
        Aplicar();
    }

    /// <summary>
    /// Libera el pedido de "quien". Si nadie más lo está pidiendo,
    /// el cursor vuelve a bloquearse automáticamente para el gameplay normal.
    /// </summary>
    public static void Liberar(object quien)
    {
        AsegurarInstancia();
        _demandantes.Remove(quien);
        Aplicar();
    }

    /// <summary>
    /// True si CUALQUIER sistema (diálogo, celular, pausa, cinemática, etc.)
    /// necesita el cursor libre en este momento. Útil para que un sistema
    /// evite reactivar el control del jugador si otro todavía lo necesita.
    /// </summary>
    public static bool CursorRequeridoLibre
    {
        get { AsegurarInstancia(); return _demandantes.Count > 0; }
    }

    // ─────────────────────────────────────────────────────────────────────
    static void Aplicar()
    {
        if (_demandantes.Count > 0)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // Si la ventana pierde y recupera el foco (clic fuera del juego,
    // alt-tab, etc.), reaplicar el estado correcto según quién siga
    // pidiendo el cursor — pase lo que pase con cualquier otro script.
    void OnApplicationFocus(bool tieneFoco)
    {
        if (tieneFoco) Aplicar();
    }
}
