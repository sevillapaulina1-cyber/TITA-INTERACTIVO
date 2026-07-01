using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Sistema de diálogo estilo iMessage para los momentos 11 y 12.
/// MODIFICADO: Añade sonidos de notificación para mensajes entrantes/salientes.
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

    [Header("── Tamaño de burbujas ──────────────────")]
    [Tooltip("Ancho fijo (en píxeles) que tendrán TODAS las burbujas, sin importar el largo del texto. " +
             "El texto hará salto de línea (wrap) dentro de este ancho.")]
    public float anchoBurbujaFijo = 900f;

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

    // ── ▼ AUDIO ──────────────────────────────────────────────────────────
    [Header("── Audio Celular ────────────────────────")]
    [Tooltip("Componente SonidoNPC con los clips de notificación. Se busca automáticamente.")]
    public SonidoNPC sonidoNPC;

    [Tooltip("AudioSource independiente para la notificación ambiental del Momento 11 " +
             "(suena ANTES de que el jugador abra el chat, mientras explora). Se crea automáticamente.")]
    public AudioSource fuenteAmbiente;

    [Tooltip("Segundos desde que el Momento 11 se activa hasta la primera notificación ambiental.")]
    [Range(0.5f, 5f)]
    public float retrasoNotificacionAmbiental = 1.5f;

    [Tooltip("Cuántas veces repetir la notificación ambiental hasta que el jugador abra el chat. " +
             "0 = solo una vez.")]
    [Range(0, 5)]
    public int repeticionesNotificacion = 2;

    [Tooltip("Segundos entre cada repetición de la notificación ambiental.")]
    [Range(1f, 10f)]
    public float intervaloRepeticion = 4f;
    // ── ▲ AUDIO ──────────────────────────────────────────────────────────

    // ── Estado interno ────────────────────────────────────────────────────
    bool _puedeInteractuar = true;
    bool _chatAbierto = false;   // true mientras el panel del celular está visible
    float _tiempoEscritura = 0.025f;
    Coroutine _coroutineAmbiental; // notificación ambiental antes de abrir el chat

    // ─────────────────────────────────────────────────────────────────────
    void OnApplicationFocus(bool tieneFoco)
    {
        // Al volver a la ventana mientras el chat está abierto,
        // forzar el cursor a libre para que el jugador pueda hacer clic.
        if (tieneFoco && _chatAbierto)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (panelCelular != null) panelCelular.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);

        // Buscar SonidoNPC automáticamente
        if (sonidoNPC == null)
            sonidoNPC = GetComponent<SonidoNPC>();
        if (sonidoNPC == null)
            sonidoNPC = GetComponentInChildren<SonidoNPC>();

        // Auto-crear AudioSource de ambiente si no se asignó en el Inspector
        if (fuenteAmbiente == null)
        {
            fuenteAmbiente = gameObject.AddComponent<AudioSource>();
            fuenteAmbiente.playOnAwake = false;
            fuenteAmbiente.loop = false;
            fuenteAmbiente.spatialBlend = 0f; // 2D — se escucha igual sin importar posición
        }

        // Lanzar notificación ambiental si el Momento 11 ya está activo al iniciar la escena
        if (GameManager.Instance != null && GameManager.Instance.MomentoActual + 1 == momentoIndex)
            _coroutineAmbiental = StartCoroutine(NotificacionAmbientalCO());
    }

    // ── ▼ AUDIO AMBIENTAL ────────────────────────────────────────────────
    /// <summary>
    /// Suena el clipNotificacion de forma repetida mientras el jugador explora,
    /// simulando que el celular está recibiendo mensajes antes de que lo abra.
    /// Se detiene automáticamente en AbrirChatCO cuando el jugador presiona E.
    /// </summary>
    IEnumerator NotificacionAmbientalCO()
    {
        if (sonidoNPC == null || sonidoNPC.clipNotificacion == null) yield break;

        yield return new WaitForSeconds(retrasoNotificacionAmbiental);

        for (int i = 0; i <= repeticionesNotificacion; i++)
        {
            if (_chatAbierto) yield break; // el jugador ya abrió el chat, parar
            fuenteAmbiente.PlayOneShot(sonidoNPC.clipNotificacion, sonidoNPC.volumenVoz);
            if (i < repeticionesNotificacion)
                yield return new WaitForSeconds(intervaloRepeticion);
        }
    }
    // ── ▲ AUDIO AMBIENTAL ────────────────────────────────────────────────

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (!_puedeInteractuar) return;
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.MomentoActual + 1 != momentoIndex) return;

        // Lanzar notificación ambiental la primera vez que el Momento 11 queda activo
        // (cubre el caso en que el momento se desbloquea después del Start)
        if (_coroutineAmbiental == null && !_chatAbierto)
            _coroutineAmbiental = StartCoroutine(NotificacionAmbientalCO());

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

        // Detener la notificación ambiental — el jugador ya abrió el chat
        if (_coroutineAmbiental != null) { StopCoroutine(_coroutineAmbiental); _coroutineAmbiental = null; }

        yield return new WaitForSeconds(0.3f);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _chatAbierto = true;

        LimpiarMensajes();

        if (panelCelular != null) panelCelular.SetActive(true);
        if (headerNombre != null) headerNombre.text = "Kid";

        yield return new WaitForSeconds(0.4f);

        // ── ▼ AUDIO: notificación al recibir el primer mensaje (NUEVO) ───
        if (sonidoNPC != null) sonidoNPC.TocarNotificacion();
        // ── ▲ AUDIO ──────────────────────────────────────────────────────

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

        // ── ▼ AUDIO: sonido de mensaje enviado (NUEVO) ────────────────────
        if (sonidoNPC != null) sonidoNPC.TocarMensajeEnviado();
        // ── ▲ AUDIO ──────────────────────────────────────────────────────

        yield return MostrarBurbujaJugador(textoJugador);
        yield return new WaitForSeconds(0.8f);

        if (!string.IsNullOrEmpty(respuestaNPC))
        {
            // ── ▼ AUDIO: notificación respuesta NPC (NUEVO) ───────────────
            if (sonidoNPC != null) sonidoNPC.TocarNotificacion();
            // ── ▲ AUDIO ──────────────────────────────────────────────────

            yield return MostrarBurbujaNPC(respuestaNPC);
            yield return new WaitForSeconds(1.2f);
        }

        GameManager.Instance.RegistrarEleccion(tipo);
        yield return TransicionMomento12CO();
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator TransicionMomento12CO()
    {
        yield return new WaitForSeconds(pausaEntreMomentos);

        // ── ▼ AUDIO: notificación momento 12 (NUEVO) ─────────────────────
        if (sonidoNPC != null) sonidoNPC.TocarNotificacion();
        // ── ▲ AUDIO ──────────────────────────────────────────────────────

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

        // ── ▼ AUDIO: sonido de mensaje enviado (NUEVO) ────────────────────
        if (sonidoNPC != null) sonidoNPC.TocarMensajeEnviado();
        // ── ▲ AUDIO ──────────────────────────────────────────────────────

        yield return MostrarBurbujaJugador(textoJugador);
        yield return new WaitForSeconds(0.8f);

        if (!string.IsNullOrEmpty(respuestaNPC))
        {
            // ── ▼ AUDIO: notificación respuesta NPC (NUEVO) ───────────────
            if (sonidoNPC != null) sonidoNPC.TocarNotificacion();
            // ── ▲ AUDIO ──────────────────────────────────────────────────

            yield return MostrarBurbujaNPC(respuestaNPC);
            yield return new WaitForSeconds(1.2f);
        }

        yield return new WaitForSeconds(0.5f);
        _chatAbierto = false;
        if (panelCelular != null) panelCelular.SetActive(false);

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
        AjustarAnchoBurbuja(burbuja, textoUI);
        textoUI.text = "";
        yield return new WaitForSeconds(0.15f);

        // En el celular (momentos 11-12) NO se reproduce voz de NPC,
        // solo los sonidos de notificación disparados antes de este método.
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
        if (textoUI != null)
        {
            AjustarAnchoBurbuja(burbuja, textoUI);
            textoUI.text = mensaje;
        }
        ScrollAlFinal();
        yield return new WaitForSeconds(0.1f);
        ScrollAlFinal();
    }

    /// <summary>
    /// Fuerza que la burbuja tenga siempre el mismo ancho (anchoBurbujaFijo),
    /// sin importar cuánto texto contenga. El texto hace salto de línea (wrap)
    /// dentro de ese ancho, y la burbuja crece solo en altura (vertical).
    /// </summary>
    void AjustarAnchoBurbuja(GameObject burbuja, Text textoUI)
    {
        // Activar wrap horizontal en el texto para que no se salga del ancho fijo
        textoUI.horizontalOverflow = HorizontalWrapMode.Wrap;
        textoUI.verticalOverflow = VerticalWrapMode.Overflow;

        // Si el prefab tiene un ContentSizeFitter, dejar que solo la altura
        // se ajuste al contenido — el ancho queda fijo/manual
        ContentSizeFitter fitterBurbuja = burbuja.GetComponent<ContentSizeFitter>();
        if (fitterBurbuja != null)
            fitterBurbuja.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        ContentSizeFitter fitterTexto = textoUI.GetComponent<ContentSizeFitter>();
        if (fitterTexto != null)
            fitterTexto.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Si hay un LayoutElement en la burbuja, fijar su ancho preferido
        LayoutElement layoutBurbuja = burbuja.GetComponent<LayoutElement>();
        if (layoutBurbuja == null) layoutBurbuja = burbuja.AddComponent<LayoutElement>();
        layoutBurbuja.preferredWidth = anchoBurbujaFijo;
        layoutBurbuja.flexibleWidth = 0f;

        // Fijar el ancho directo del RectTransform de la burbuja (por si no usa LayoutElement)
        RectTransform rectBurbuja = burbuja.GetComponent<RectTransform>();
        if (rectBurbuja != null)
        {
            Vector2 size = rectBurbuja.sizeDelta;
            size.x = anchoBurbujaFijo;
            rectBurbuja.sizeDelta = size;
        }

        // También fijar el ancho del RectTransform del texto para que el wrap
        // se calcule sobre el ancho correcto (dejando margen para padding)
        RectTransform rectTexto = textoUI.GetComponent<RectTransform>();
        if (rectTexto != null && rectTexto != rectBurbuja)
        {
            Vector2 sizeTexto = rectTexto.sizeDelta;
            sizeTexto.x = anchoBurbujaFijo - 40f; // ajustá el -40 según el padding de tu burbuja
            rectTexto.sizeDelta = sizeTexto;
        }
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
