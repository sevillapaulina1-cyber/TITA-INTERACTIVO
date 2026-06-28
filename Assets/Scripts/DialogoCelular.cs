using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Sistema de diálogo estilo iMessage para los momentos 11 y 12.
/// CORREGIDO v2:
///  - Mensajes aparecen instantáneamente (sin typewriter)
///  - Auto-scroll al fondo después de cada burbuja
///  - Burbujas se ajustan al texto via ContentSizeFitter (configurar en prefab)
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
    [Tooltip("Pausa antes del primer mensaje (para que el chat 'llegue')")]
    public float pausaAntesDelMensaje = 0.5f;
    [Tooltip("Pausa entre la respuesta del NPC y el siguiente momento")]
    public float pausaEntreMomentos = 1.5f;
    [Tooltip("Pausa entre mensaje del jugador y respuesta del NPC")]
    public float pausaRespuestaNPC = 0.8f;

    [Header("── Audio Celular ────────────────────────")]
    [Tooltip("Componente SonidoNPC con los clips de notificación. Se busca automáticamente.")]
    public SonidoNPC sonidoNPC;

    // ── Estado interno ────────────────────────────────────────────────────
    bool _puedeInteractuar = true;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (panelCelular != null) panelCelular.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);

        if (sonidoNPC == null) sonidoNPC = GetComponent<SonidoNPC>();
        if (sonidoNPC == null) sonidoNPC = GetComponentInChildren<SonidoNPC>();
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
        if (headerNombre != null) headerNombre.text = "SamuVR";

        yield return new WaitForSeconds(pausaAntesDelMensaje);

        if (sonidoNPC != null) sonidoNPC.TocarNotificacion();

        yield return MostrarBurbujaNPC(mensajeInicial11);

        // Pequeña pausa natural antes de mostrar opciones
        yield return new WaitForSeconds(0.4f);

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

        if (sonidoNPC != null) sonidoNPC.TocarMensajeEnviado();

        yield return MostrarBurbujaJugador(textoJugador);
        yield return new WaitForSeconds(pausaRespuestaNPC);

        if (!string.IsNullOrEmpty(respuestaNPC))
        {
            if (sonidoNPC != null) sonidoNPC.TocarNotificacion();
            yield return MostrarBurbujaNPC(respuestaNPC);
            yield return new WaitForSeconds(1.0f);
        }

        GameManager.Instance.RegistrarEleccion(tipo);
        yield return TransicionMomento12CO();
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator TransicionMomento12CO()
    {
        yield return new WaitForSeconds(pausaEntreMomentos);

        if (sonidoNPC != null) sonidoNPC.TocarNotificacion();

        yield return MostrarBurbujaNPC(mensajeInicial12);
        yield return new WaitForSeconds(0.4f);

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

        if (sonidoNPC != null) sonidoNPC.TocarMensajeEnviado();

        yield return MostrarBurbujaJugador(textoJugador);
        yield return new WaitForSeconds(pausaRespuestaNPC);

        if (!string.IsNullOrEmpty(respuestaNPC))
        {
            if (sonidoNPC != null) sonidoNPC.TocarNotificacion();
            yield return MostrarBurbujaNPC(respuestaNPC);
            yield return new WaitForSeconds(1.0f);
        }

        yield return new WaitForSeconds(0.5f);
        if (panelCelular != null) panelCelular.SetActive(false);

        GameManager.Instance.RegistrarEleccion(tipo);
        this.enabled = false;

        // Restaurar cursor y controlador
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (firstPersonController != null) firstPersonController.enabled = true;
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

        boton1?.onClick.RemoveAllListeners(); boton1?.onClick.AddListener(cb1);
        boton2?.onClick.RemoveAllListeners(); boton2?.onClick.AddListener(cb2);
        boton3?.onClick.RemoveAllListeners(); boton3?.onClick.AddListener(cb3);

        if (panelOpciones != null) panelOpciones.SetActive(true);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Burbuja NPC: aparece instantáneamente con el texto completo
    // ─────────────────────────────────────────────────────────────────────
    IEnumerator MostrarBurbujaNPC(string mensaje)
    {
        if (prefabBurbujaNPC == null || contenedorMensajes == null) yield break;

        GameObject burbuja = Instantiate(prefabBurbujaNPC, contenedorMensajes);
        Text textoUI = burbuja.GetComponentInChildren<Text>();
        if (textoUI != null) textoUI.text = mensaje;

        // Esperar DOS frames para que Unity recalcule el layout y el ContentSizeFitter
        // ajuste la altura de la burbuja antes de hacer scroll
        yield return null;
        yield return null;

        ScrollAlFinal();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Burbuja Jugador: aparece instantáneamente
    // ─────────────────────────────────────────────────────────────────────
    IEnumerator MostrarBurbujaJugador(string mensaje)
    {
        if (prefabBurbujaJugador == null || contenedorMensajes == null) yield break;

        GameObject burbuja = Instantiate(prefabBurbujaJugador, contenedorMensajes);
        Text textoUI = burbuja.GetComponentInChildren<Text>();
        if (textoUI != null) textoUI.text = mensaje;

        yield return null;
        yield return null;

        ScrollAlFinal();
    }

    // ─────────────────────────────────────────────────────────────────────
    void ScrollAlFinal()
    {
        if (scrollRect == null) return;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contenedorMensajes as RectTransform);
        scrollRect.verticalNormalizedPosition = 0f;
    }

    void LimpiarMensajes()
    {
        if (contenedorMensajes == null) return;
        foreach (Transform hijo in contenedorMensajes)
            Destroy(hijo.gameObject);
    }
}
