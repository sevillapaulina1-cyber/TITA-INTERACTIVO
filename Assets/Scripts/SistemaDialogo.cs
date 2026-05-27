using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Sistema de diálogo reutilizable para los 12 momentos.
/// Configurable completamente desde el Inspector.
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

    [Header("── Botones de opciones ────────────────")]
    public Text textoBoton1;
    public Text textoBoton2;
    public Text textoBoton3;

    [Header("── Contenido del diálogo ──────────────")]
    [TextArea] public string textoYo = "¡Hola!";
    [TextArea] public string textoNPC = "...";

    [Header("── Opción 1 (Verde) ───────────────────")]
    [TextArea] public string textoChoice1 = "Opción 1";
    public TipoEleccion tipoChoice1 = TipoEleccion.Verde;
    [TextArea] public string respuestaNPCChoice1 = "";  // lo que responde el NPC tras elegir esta opción

    [Header("── Opción 2 (Neutro) ──────────────────")]
    [TextArea] public string textoChoice2 = "Opción 2";
    public TipoEleccion tipoChoice2 = TipoEleccion.Neutro;
    [TextArea] public string respuestaNPCChoice2 = "";

    [Header("── Opción 3 (Rojo) ────────────────────")]
    [TextArea] public string textoChoice3 = "Opción 3";
    public TipoEleccion tipoChoice3 = TipoEleccion.Rojo;
    [TextArea] public string respuestaNPCChoice3 = "";

    [Header("── Recolector de monedas (opcional) ───")]
    [Tooltip("Asigna el RecolectorMonedas que debe completarse ANTES de este diálogo. Solo en momentos que usen recolector simple.")]
    public RecolectorMonedas recolectorPrevio;

    [Header("── Puzzle de palancas (opcional) ───────")]
    [Tooltip("Asigna el PuzzlePalancas que debe completarse ANTES de este diálogo. Usar en momento 5.")]
    public PuzzlePalancas puzzlePrevio;

    [Header("── Zoom Out (solo activar en Momento 8) ─")]
    public bool hacerZoomOut = false;
    public Camera camara;
    public float fovInicial = 60f;
    public float fovFinal = 90f;
    public float duracionZoom = 2.0f;
    public float esperaZoom = 1.5f;

    // ── Estado interno ────────────────────────────────────────────────────
    bool _puedeInteractuar = true;
    float _time = 0.05f;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (npcTransform != null && posicionNPCEsteDia != null)
            npcTransform.position = posicionNPCEsteDia.position;
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
                    // Bloquear si hay tarea de monedas pendiente
                    if (recolectorPrevio != null && recolectorPrevio.TareaPendiente())
                    {
                        if (interactionText != null)
                            interactionText.text = "Recoge todas las monedas primero";
                        return;
                    }

                    // Bloquear si el puzzle de palancas está pendiente
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

        // El jugador responde
        yield return EscribirTexto("Yo: ", textoRespuesta);
        yield return new WaitForSeconds(0.5f);

        // El NPC responde (solo si hay texto asignado)
        if (!string.IsNullOrEmpty(respuestaNPC))
        {
            yield return PresionarMouse();
            yield return EscribirTexto("Kid: ", respuestaNPC);
            yield return new WaitForSeconds(1f);
        }
        else
        {
            yield return new WaitForSeconds(2f);
        }

        // Cerrar UI
        talkPanel.SetActive(false);
        talkText.SetActive(false);
        subText.text = "";

        // Registrar elección
        GameManager.Instance.RegistrarEleccion(tipo);

        bool esUltimoMomento = GameManager.Instance.MomentoActual >= GameManager.TOTAL_MOMENTOS;
        if (!esUltimoMomento)
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

