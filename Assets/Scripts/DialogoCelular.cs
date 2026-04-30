using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Sistema de diálogo estilo iMessage para los momentos 11 y 12.
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

    [Header("── Contenido del chat ─────────────────")]
    [TextArea] public string mensajeInicial = "...";

    [Header("── Opción 1 (Verde) ───────────────────")]
    [TextArea] public string textoChoice1 = "Opción 1";
    public TipoEleccion tipoChoice1 = TipoEleccion.Verde;
    [TextArea] public string respuestaNPCChoice1 = "";

    [Header("── Opción 2 (Neutro) ──────────────────")]
    [TextArea] public string textoChoice2 = "Opción 2";
    public TipoEleccion tipoChoice2 = TipoEleccion.Neutro;
    [TextArea] public string respuestaNPCChoice2 = "";

    [Header("── Opción 3 (Rojo) ────────────────────")]
    [TextArea] public string textoChoice3 = "Opción 3";
    public TipoEleccion tipoChoice3 = TipoEleccion.Rojo;
    [TextArea] public string respuestaNPCChoice3 = "";

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

        LimpiarMensajes();

        if (panelCelular != null) panelCelular.SetActive(true);
        if (headerNombre != null) headerNombre.text = "Kid";

        yield return new WaitForSeconds(0.4f);

        yield return MostrarBurbujaNPC(mensajeInicial);

        yield return new WaitForSeconds(0.6f);

        if (textoBoton1 != null) textoBoton1.text = textoChoice1;
        if (textoBoton2 != null) textoBoton2.text = textoChoice2;
        if (textoBoton3 != null) textoBoton3.text = textoChoice3;

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

        yield return MostrarBurbujaJugador(textoJugador);
        yield return new WaitForSeconds(0.8f);

        if (!string.IsNullOrEmpty(respuestaNPC))
        {
            yield return MostrarBurbujaNPC(respuestaNPC);
            yield return new WaitForSeconds(1.2f);
        }

        yield return new WaitForSeconds(0.5f);
        if (panelCelular != null) panelCelular.SetActive(false);

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

    // ─────────────────────────────────────────────────────────────────────
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

    // ─────────────────────────────────────────────────────────────────────
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

