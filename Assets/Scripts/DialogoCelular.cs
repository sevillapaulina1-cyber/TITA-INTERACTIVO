using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Sistema de diálogo estilo iMessage para los momentos 11 y 12.
/// El jugador presiona E para abrir el chat (momento 11).
/// Al elegir en el 11, el momento 12 llega automáticamente sin cerrar el chat.
/// Al elegir en el 12, se cierra y el GameManager evalúa el final.
/// </summary>
public class DialogoCelular : MonoBehaviour
{
    [Header("── Identificación ──────────────────────")]
    [Tooltip("Dejar en 11 — gestiona el 11 y 12 seguidos")]
    public int momentoIndex = 11;

    [Header("── Referencia al jugador ───────────────")]
    public Transform jugador;
    public MonoBehaviour firstPersonController;
    public float distanciaInteraccion = 5f;
    public Text interactionText;

    [Header("── UI del celular ──────────────────────")]
    public GameObject panelCelular;
    public Text headerNombre;
    public Transform contenedorMensajes;
    public ScrollRect scrollRect;
    public GameObject panelOpciones;

    [Header("── Prefabs de burbujas ────────────────")]
    public GameObject prefabBurbujaNPC;
    public GameObject prefabBurbujaJugador;

    [Header("── Botones ─────────────────────────────")]
    public Button boton1;
    public Button boton2;
    public Button boton3;
    public Text textoBoton1;
    public Text textoBoton2;
    public Text textoBoton3;

    // ══════════════════════════════════════════════════════════════════════
    // MOMENTO 11
    // ══════════════════════════════════════════════════════════════════════
    [Header("── Momento 11 ───────────────────────────")]
    [TextArea] public string mensajeInicial11 = "No le digo a otros que hablo contigo";

    [TextArea] public string textoChoice11_1 = "No pasa nada";
    public TipoEleccion tipoChoice11_1 = TipoEleccion.Verde;
    [TextArea] public string respNPC11_1 = "Sí... igual me alegra poder hablar contigo";

    [TextArea] public string textoChoice11_2 = "Es mejor que sea entre nosotros";
    public TipoEleccion tipoChoice11_2 = TipoEleccion.Neutro;
    [TextArea] public string respNPC11_2 = "Sí, exacto. Tú me entiendes";

    [TextArea] public string textoChoice11_3 = "Sí, es nuestro secreto";
    public TipoEleccion tipoChoice11_3 = TipoEleccion.Rojo;
    [TextArea] public string respNPC11_3 = "Me alegra mucho... eres especial para mí";

    // ══════════════════════════════════════════════════════════════════════
    // MOMENTO 12
    // ══════════════════════════════════════════════════════════════════════
    [Header("── Momento 12 ───────────────────────────")]
    [TextArea] public string mensajeInicial12 = "Oye... ¿podríamos vernos en persona?";

    [TextArea] public string textoChoice12_1 = "No creo que sea buena idea";
    public TipoEleccion tipoChoice12_1 = TipoEleccion.Verde;
    [TextArea] public string respNPC12_1 = "...";

    [TextArea] public string textoChoice12_2 = "No sé... tengo que pensar";
    public TipoEleccion tipoChoice12_2 = TipoEleccion.Neutro;
    [TextArea] public string respNPC12_2 = "Claro, tómate tu tiempo";

    [TextArea] public string textoChoice12_3 = "Sí, me gustaría conocerte";
    public TipoEleccion tipoChoice12_3 = TipoEleccion.Rojo;
    [TextArea] public string respNPC12_3 = "Perfecto... te mando la dirección";

    [Header("── Tiempos ──────────────────────────────")]
    [Tooltip("Pausa antes de que llegue el mensaje del momento 12")]
    public float pausaEntreMomentos = 2.0f;

    // ── Estado interno ────────────────────────────────────────────────────
    bool _puedeInteractuar = true;
    float _tiempoEscritura = 0.025f;

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
            else { if (interactionText != null) interactionText.text = ""; }
        }
        else { if (interactionText != null) interactionText.text = ""; }
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator AbrirChatCO()
    {
        if (interactionText != null) interactionText.text = "";
        firstPersonController.enabled = false;

        yield return new WaitForSeconds(0.3f);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        LimpiarMensajes();

        if (panelCelular != null) panelCelular.SetActive(true);
        if (headerNombre != null) headerNombre.text = "Kid";

        yield return new WaitForSeconds(0.4f);
        yield return MostrarBurbujaNPC(mensajeInicial11);
        yield return new WaitForSeconds(0.6f);

        MostrarOpciones(textoChoice11_1, textoChoice11_2, textoChoice11_3,
                        Choice11_1, Choice11_2, Choice11_3);
    }

    // ─── Elecciones Momento 11 ────────────────────────────────────────────
    void Choice11_1() => StartCoroutine(EleccionMomento11CO(tipoChoice11_1, textoChoice11_1, respNPC11_1));
    void Choice11_2() => StartCoroutine(EleccionMomento11CO(tipoChoice11_2, textoChoice11_2, respNPC11_2));
    void Choice11_3() => StartCoroutine(EleccionMomento11CO(tipoChoice11_3, textoChoice11_3, respNPC11_3));

    IEnumerator EleccionMomento11CO(TipoEleccion tipo, string textoJugador, string respuestaNPC)
    {
        if (panelOpciones != null) panelOpciones.SetActive(false);

        yield return MostrarBurbujaJugador(textoJugador);
        yield return new WaitForSeconds(0.8f);

        if (!string.IsNullOrEmpty(respuestaNPC))
        {
            yield return MostrarBurbujaNPC(respuestaNPC);
            yield return new WaitForSeconds(1.2f);
        }

        // Registrar momento 11 y pasar directo al 12 sin cerrar el chat
        GameManager.Instance.RegistrarEleccion(tipo);
        yield return TransicionMomento12CO();
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator TransicionMomento12CO()
    {
        // Pausa como si el NPC estuviera escribiendo
        yield return new WaitForSeconds(pausaEntreMomentos);

        yield return MostrarBurbujaNPC(mensajeInicial12);
        yield return new WaitForSeconds(0.6f);

        MostrarOpciones(textoChoice12_1, textoChoice12_2, textoChoice12_3,
                        Choice12_1, Choice12_2, Choice12_3);
    }

    // ─── Elecciones Momento 12 ────────────────────────────────────────────
    void Choice12_1() => StartCoroutine(EleccionMomento12CO(tipoChoice12_1, textoChoice12_1, respNPC12_1));
    void Choice12_2() => StartCoroutine(EleccionMomento12CO(tipoChoice12_2, textoChoice12_2, respNPC12_2));
    void Choice12_3() => StartCoroutine(EleccionMomento12CO(tipoChoice12_3, textoChoice12_3, respNPC12_3));

    IEnumerator EleccionMomento12CO(TipoEleccion tipo, string textoJugador, string respuestaNPC)
    {
        if (panelOpciones != null) panelOpciones.SetActive(false);

        yield return MostrarBurbujaJugador(textoJugador);
        yield return new WaitForSeconds(0.8f);

        if (!string.IsNullOrEmpty(respuestaNPC))
        {
            yield return MostrarBurbujaNPC(respuestaNPC);
            yield return new WaitForSeconds(1.2f);
        }

        yield return new WaitForSeconds(0.5f);
        if (panelCelular != null) panelCelular.SetActive(false);

        // Registrar momento 12 — dispara EvaluarFinal en GameManager
        GameManager.Instance.RegistrarEleccion(tipo);

        this.enabled = false;
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    void MostrarOpciones(string txt1, string txt2, string txt3,
                         UnityEngine.Events.UnityAction cb1,
                         UnityEngine.Events.UnityAction cb2,
                         UnityEngine.Events.UnityAction cb3)
    {
        if (textoBoton1 != null) textoBoton1.text = txt1;
        if (textoBoton2 != null) textoBoton2.text = txt2;
        if (textoBoton3 != null) textoBoton3.text = txt3;

        if (boton1 != null) { boton1.onClick.RemoveAllListeners(); boton1.onClick.AddListener(cb1); }
        if (boton2 != null) { boton2.onClick.RemoveAllListeners(); boton2.onClick.AddListener(cb2); }
        if (boton3 != null) { boton3.onClick.RemoveAllListeners(); boton3.onClick.AddListener(cb3); }

        if (panelOpciones != null) panelOpciones.SetActive(true);
    }

    IEnumerator MostrarBurbujaNPC(string mensaje)
    {
        if (prefabBurbujaNPC == null || contenedorMensajes == null) yield break;
        GameObject burbuja = Instantiate(prefabBurbujaNPC, contenedorMensajes);
        Text textoUI = burbuja.GetComponentInChildren<Text>();
        if (textoUI == null) yield break;
        textoUI.text = "";
        yield return new WaitForSeconds(0.15f);
        foreach (char c in mensaje)
        {
            textoUI.text += c;
            ScrollAlFinal();
            yield return new WaitForSeconds(_tiempoEscritura);
        }
        ScrollAlFinal();
    }

    IEnumerator MostrarBurbujaJugador(string mensaje)
    {
        if (prefabBurbujaJugador == null || contenedorMensajes == null) yield break;
        GameObject burbuja = Instantiate(prefabBurbujaJugador, contenedorMensajes);
        Text textoUI = burbuja.GetComponentInChildren<Text>();
        if (textoUI != null) textoUI.text = mensaje;
        ScrollAlFinal();
        yield return new WaitForSeconds(0.1f);
        ScrollAlFinal();
    }

    void ScrollAlFinal()
    {
        if (scrollRect == null) return;
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    void LimpiarMensajes()
    {
        if (contenedorMensajes == null) return;
        foreach (Transform hijo in contenedorMensajes)
            Destroy(hijo.gameObject);
    }
}

