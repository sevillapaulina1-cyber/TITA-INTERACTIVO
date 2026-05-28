using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Sistema de diálogo reutilizable para los 12 momentos.
/// Los botones se asignan automáticamente por código al abrir el diálogo.
/// Solo necesitas asignar los 3 Buttons (no los OnClick) en el Inspector.
/// </summary>
public class SistemaDialogo : MonoBehaviour
{
    [Header("── Identificación ──────────────────────")]
    [Tooltip("Número de este momento (1 al 12)")]
    public int momentoIndex = 1;

    [Header("── Referencia al jugador ───────────────")]
    public Transform jugador;
    public MonoBehaviour firstPersonController;
    public float distanciaInteraccion = 5f;

    [Header("── NPC ─────────────────────────────────")]
    public Transform npcTransform;
    public Transform posicionNPCEsteDia;

    [Header("── UI global ───────────────────────────")]
    public Text interactionText;
    public GameObject talkPanel;
    public GameObject choicePack;
    public GameObject talkText;
    public Text subText;

    [Header("── Botones (asigna el Button, no el OnClick) ──")]
    public Button boton1;
    public Button boton2;
    public Button boton3;
    public Text textoBoton1;
    public Text textoBoton2;
    public Text textoBoton3;

    [Header("── Contenido del diálogo ──────────────")]
    [TextArea] public string textoYo = "¡Hola!";
    [TextArea] public string textoNPC = "...";

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

    [Header("── Bloqueos previos (opcional) ─────────")]
    public RecolectorMonedas recolectorPrevio;
    public PuzzlePalancas puzzlePrevio;

    [Header("── Zoom Out (solo Momento 8) ───────────")]
    public bool hacerZoomOut = false;
    public Camera camara;
    public float fovInicial = 60f;
    public float fovFinal = 90f;
    public float duracionZoom = 2.0f;
    public float esperaZoom = 1.5f;

    // ── Estado interno ────────────────────────────────────────────────────
    bool _puedeInteractuar = true;
    bool _botonesAsignados = false;
    float _time = 0.05f;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (npcTransform != null && posicionNPCEsteDia != null)
            npcTransform.position = posicionNPCEsteDia.position;
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Asigna los listeners de los botones a ESTE momento.
    /// Se llama justo antes de mostrar las opciones.
    /// Limpia listeners anteriores para evitar acumulación.
    /// </summary>
    void AsignarBotones()
    {
        if (boton1 != null)
        {
            boton1.onClick.RemoveAllListeners();
            boton1.onClick.AddListener(Choice1Void);
        }
        if (boton2 != null)
        {
            boton2.onClick.RemoveAllListeners();
            boton2.onClick.AddListener(Choice2Void);
        }
        if (boton3 != null)
        {
            boton3.onClick.RemoveAllListeners();
            boton3.onClick.AddListener(Choice3Void);
        }

        _botonesAsignados = true;
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
                    interactionText.text = "Presiona E para hablar";

                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    if (recolectorPrevio != null && recolectorPrevio.TareaPendiente())
                    {
                        if (interactionText != null)
                            interactionText.text = "Recoge todas las monedas primero";
                        return;
                    }

                    if (puzzlePrevio != null && puzzlePrevio.PuzzlePendiente())
                    {
                        if (interactionText != null)
                            interactionText.text = "Completa el puzzle primero";
                        return;
                    }

                    _puedeInteractuar = false;
                    StartCoroutine(DialogoCO());
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
    IEnumerator DialogoCO()
    {
        if (interactionText != null) interactionText.text = "";
        firstPersonController.enabled = false;

        yield return new WaitForSeconds(0.5f);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        talkPanel.SetActive(true);
        talkText.SetActive(true);

        // Actualizar textos de botones
        if (textoBoton1 != null) textoBoton1.text = textoChoice1;
        if (textoBoton2 != null) textoBoton2.text = textoChoice2;
        if (textoBoton3 != null) textoBoton3.text = textoChoice3;

        yield return EscribirTexto("Yo: ", textoYo);
        yield return PresionarMouse();
        yield return EscribirTexto("Kid: ", textoNPC);

        yield return new WaitForSeconds(0.8f);

        if (hacerZoomOut && camara != null)
        {
            yield return ZoomOutCO();
            yield return new WaitForSeconds(esperaZoom);
        }

        // Asignar botones a ESTE momento justo antes de mostrarlos
        AsignarBotones();
        choicePack.SetActive(true);
    }

    // ─── Botones ──────────────────────────────────────────────────────────
    public void Choice1Void() => StartCoroutine(EleccionCO(tipoChoice1, textoChoice1, respuestaNPCChoice1));
    public void Choice2Void() => StartCoroutine(EleccionCO(tipoChoice2, textoChoice2, respuestaNPCChoice2));
    public void Choice3Void() => StartCoroutine(EleccionCO(tipoChoice3, textoChoice3, respuestaNPCChoice3));

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator EleccionCO(TipoEleccion tipo, string textoRespuesta, string respuestaNPC)
    {
        choicePack.SetActive(false);

        yield return EscribirTexto("Yo: ", textoRespuesta);
        yield return new WaitForSeconds(0.5f);

        if (!string.IsNullOrEmpty(respuestaNPC))
        {
            yield return new WaitForSeconds(0.4f);
            yield return EscribirTexto("Kid: ", respuestaNPC);
            yield return new WaitForSeconds(1.5f);
        }
        else
        {
            yield return new WaitForSeconds(1.5f);
        }

        talkPanel.SetActive(false);
        talkText.SetActive(false);
        subText.text = "";

        GameManager.Instance.RegistrarEleccion(tipo);

        bool esUltimo = GameManager.Instance.MomentoActual >= GameManager.TOTAL_MOMENTOS;
        if (!esUltimo)
        {
            firstPersonController.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        this.enabled = false;
        yield return null;
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator ZoomOutCO()
    {
        float t = 0f;
        camara.fieldOfView = fovInicial;
        while (t < duracionZoom)
        {
            t += Time.deltaTime;
            camara.fieldOfView = Mathf.Lerp(fovInicial, fovFinal, t / duracionZoom);
            yield return null;
        }
        camara.fieldOfView = fovFinal;
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator EscribirTexto(string hablante, string mensaje)
    {
        subText.text = hablante;
        foreach (char c in mensaje)
        {
            subText.text += c;
            yield return new WaitForSeconds(_time);
        }
    }

    IEnumerator PresionarMouse()
    {
        while (!Mouse.current.leftButton.wasPressedThisFrame)
            yield return null;
    }
}