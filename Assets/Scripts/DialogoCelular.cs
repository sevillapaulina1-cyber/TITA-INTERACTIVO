using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Sistema de diálogo tipo chat de celular para el Momento 11.
/// Muestra burbujas de chat — NPC a la izquierda, jugador a la derecha.
///
/// SETUP EN UNITY:
///   1. En el Canvas crea un panel "PanelCelular" con este aspecto:
///      - Fondo oscuro semitransparente que cubre la pantalla
///      - Dentro: un panel central estilo celular (ancho ~400px, alto ~600px)
///        con fondo blanco/gris oscuro, bordes redondeados
///        ┌─────────────────────┐
///        │  [Header: Kid 🟢]  │  ← headerNombre (Text)
///        ├─────────────────────┤
///        │  [ScrollView]       │  ← contenedorMensajes (el Content del ScrollRect)
///        │    burbujas aquí    │
///        ├─────────────────────┤
///        │  [Opciones A B C]   │  ← panelOpciones (GameObject)
///        └─────────────────────┘
///
///   2. Prefab de burbuja NPC  (BurbujaNPC):
///      - HorizontalLayoutGroup alineado a la izquierda
///      - Image redondeada color gris claro
///      - Text dentro
///   3. Prefab de burbuja Jugador (BurbujaJugador):
///      - HorizontalLayoutGroup alineado a la derecha
///      - Image redondeada color verde/azul
///      - Text dentro
///
///   4. Este script va en un GameObject vacío "DialogoCelular_11".
///      MomentoIndex = 11, configurar textos en el Inspector.
/// </summary>
public class DialogoCelular : MonoBehaviour
{
    [Header("── Identificación ──────────────────────")]
    public int momentoIndex = 11;

    [Header("── Referencia al jugador ───────────────")]
    public Transform jugador;
    public MonoBehaviour firstPersonController;
    public float distanciaInteraccion = 5f;
    public Text interactionText;

    [Header("── UI del celular ──────────────────────")]
    public GameObject panelCelular;          // panel raíz del celular
    public Text headerNombre;          // nombre en el header (ej: "Kid")
    public Transform contenedorMensajes;    // Content del ScrollRect
    public ScrollRect scrollRect;            // para hacer scroll automático
    public GameObject panelOpciones;         // panel con los 3 botones

    [Header("── Prefabs de burbujas ────────────────")]
    public GameObject prefabBurbujaNPC;      // burbuja izquierda (gris)
    public GameObject prefabBurbujaJugador;  // burbuja derecha (verde/azul)

    [Header("── Botones de opciones ────────────────")]
    public Button boton1;
    public Button boton2;
    public Button boton3;
    public Text textoBoton1;
    public Text textoBoton2;
    public Text textoBoton3;

    [Header("── Contenido del chat ─────────────────")]
    [TextArea] public string mensajeInicial = "No le digo a otros que hablo contigo";

    [Header("── Opción 1 (Verde) ───────────────────")]
    [TextArea] public string textoChoice1 = "No pasa nada";
    public TipoEleccion tipoChoice1 = TipoEleccion.Verde;
    [TextArea] public string respuestaNPCChoice1 = "Sí... igual me alegra poder hablar contigo";

    [Header("── Opción 2 (Neutro) ──────────────────")]
    [TextArea] public string textoChoice2 = "Es mejor que sea entre nosotros";
    public TipoEleccion tipoChoice2 = TipoEleccion.Neutro;
    [TextArea] public string respuestaNPCChoice2 = "Sí, exacto. Tú me entiendes";

    [Header("── Opción 3 (Rojo) ────────────────────")]
    [TextArea] public string textoChoice3 = "Sí, es nuestro secreto";
    public TipoEleccion tipoChoice3 = TipoEleccion.Rojo;
    [TextArea] public string respuestaNPCChoice3 = "Me alegra mucho... eres especial para mí";

    // ── Estado interno ────────────────────────────────────────────────────
    bool _puedeInteractuar = true;
    bool _chatAbierto = false;
    float _tiempoEscritura = 0.03f;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (panelCelular != null) panelCelular.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (!_puedeInteractuar) return;
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.MomentoActual + 1 != momentoIndex) return;

        // Mantener cursor visible si el chat está abierto
        if (_chatAbierto)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        Ray ray = new Ray(jugador.position, jugador.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, distanciaInteraccion))
        {
            if (hit.collider.CompareTag("Npc"))
            {
                if (interactionText != null)
                    interactionText.text = "Presiona E para ver el mensaje";

                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    _puedeInteractuar = false;
                    StartCoroutine(AbrirChatCO());
                }
            }
            else
            {
                if (interactionText != null) interactionText.text = "";
            }
        }
        else
        {
            if (interactionText != null) interactionText.text = "";
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator AbrirChatCO()
    {
        if (interactionText != null) interactionText.text = "";
        firstPersonController.enabled = false;

        yield return new WaitForSeconds(0.3f);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        _chatAbierto = true;

        // Abrir panel del celular
        if (panelCelular != null) panelCelular.SetActive(true);
        if (headerNombre != null) headerNombre.text = "Kid";

        // Limpiar mensajes anteriores si los hubiera
        LimpiarMensajes();

        yield return new WaitForSeconds(0.5f);

        // Mostrar mensaje inicial del NPC con efecto de "escribiendo..."
        yield return MostrarBurbujaNPC(mensajeInicial);

        yield return new WaitForSeconds(0.8f);

        // Actualizar textos de botones
        if (textoBoton1 != null) textoBoton1.text = textoChoice1;
        if (textoBoton2 != null) textoBoton2.text = textoChoice2;
        if (textoBoton3 != null) textoBoton3.text = textoChoice3;

        // Asignar OnClick dinámicamente
        if (boton1 != null) { boton1.onClick.RemoveAllListeners(); boton1.onClick.AddListener(Choice1Void); }
        if (boton2 != null) { boton2.onClick.RemoveAllListeners(); boton2.onClick.AddListener(Choice2Void); }
        if (boton3 != null) { boton3.onClick.RemoveAllListeners(); boton3.onClick.AddListener(Choice3Void); }

        if (panelOpciones != null) panelOpciones.SetActive(true);
    }

    // ─── Botones ──────────────────────────────────────────────────────────
    public void Choice1Void() => StartCoroutine(EleccionCO(tipoChoice1, textoChoice1, respuestaNPCChoice1));
    public void Choice2Void() => StartCoroutine(EleccionCO(tipoChoice2, textoChoice2, respuestaNPCChoice2));
    public void Choice3Void() => StartCoroutine(EleccionCO(tipoChoice3, textoChoice3, respuestaNPCChoice3));

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator EleccionCO(TipoEleccion tipo, string textoJugador, string respuestaNPC)
    {
        if (panelOpciones != null) panelOpciones.SetActive(false);

        // Burbuja del jugador (derecha)
        yield return MostrarBurbujaJugador(textoJugador);
        yield return new WaitForSeconds(0.8f);

        // Respuesta del NPC (izquierda)
        if (!string.IsNullOrEmpty(respuestaNPC))
        {
            yield return MostrarBurbujaNPC(respuestaNPC);
            yield return new WaitForSeconds(1.5f);
        }

        _chatAbierto = false;

        // Cerrar chat
        if (panelCelular != null) panelCelular.SetActive(false);

        // Registrar elección
        GameManager.Instance.RegistrarEleccion(tipo);

        bool esUltimo = GameManager.Instance.MomentoActual >= GameManager.TOTAL_MOMENTOS;
        if (!esUltimo)
        {
            firstPersonController.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        this.enabled = false;
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator MostrarBurbujaNPC(string mensaje)
    {
        if (prefabBurbujaNPC == null) { Debug.LogError("[Chat] prefabBurbujaNPC es null"); yield break; }

        GameObject burbuja = Instantiate(prefabBurbujaNPC, contenedorMensajes);
        Debug.Log($"[Chat] Burbuja NPC instanciada: {burbuja.name}");
        Text textoUI = burbuja.GetComponentInChildren<Text>();
        if (textoUI == null) { Debug.LogError("[Chat] No se encontró Text en BurbujaNPC"); yield break; }

        textoUI.text = "";
        ScrollAlFinal();

        foreach (char c in mensaje)
        {
            textoUI.text += c;
            ScrollAlFinal();
            yield return new WaitForSeconds(_tiempoEscritura);
        }

        ScrollAlFinal();
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator MostrarBurbujaJugador(string mensaje)
    {
        if (prefabBurbujaJugador == null) yield break;

        GameObject burbuja = Instantiate(prefabBurbujaJugador, contenedorMensajes);
        Text textoUI = burbuja.GetComponentInChildren<Text>();
        if (textoUI == null) yield break;

        textoUI.text = mensaje;
        ScrollAlFinal();

        yield return new WaitForSeconds(0.1f);
    }

    // ─────────────────────────────────────────────────────────────────────
    void ScrollAlFinal()
    {
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    void LimpiarMensajes()
    {
        if (contenedorMensajes == null) return;
        foreach (Transform hijo in contenedorMensajes)
            Destroy(hijo.gameObject);
    }
}

