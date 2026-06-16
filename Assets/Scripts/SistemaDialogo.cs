using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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

    [Header("── Botones ─────────────────────────────")]
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
    [Tooltip("Asigna si hay monedas que recoger antes de este momento")]
    public RecolectorMonedas recolectorPrevio;
    [Tooltip("Asigna GestorZonas_4a5 en el momento 5 para bloquear hasta completar el puzzle")]
    public GestorZonas gestorZonasPrevio;

    [Header("── Animación Momento 8 (Animator) ───────")]
    [Tooltip("Marca esto solo en el Momento 8")]
    public bool usarAnimacionMomento8 = false;
    [Tooltip("Animator de la cámara con la animación de paneo")]
    public Animator animatorCamara;
    [Tooltip("Nombre del trigger en el Animator que dispara la animación")]
    public string triggerAnimacion = "PaneoMomento8";
    [Tooltip("Duración de la animación en segundos (para esperar antes de continuar)")]
    public float duracionAnimacion = 6.0f;

    // ── ▼ AUDIO ── (NUEVO) ──────────────────────────────────────────────
    [Header("── Audio NPC ────────────────────────────")]
    [Tooltip("Componente SonidoNPC en este mismo GO o en el NPC. Se busca automáticamente si está vacío.")]
    public SonidoNPC sonidoNPC;
    // ── ▲ AUDIO ─────────────────────────────────────────────────────────

    bool _puedeInteractuar = true;
    float _time = 0.05f;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (npcTransform != null && posicionNPCEsteDia != null)
            npcTransform.position = posicionNPCEsteDia.position;

        if (animatorCamara != null)
            animatorCamara.enabled = false;

        // Buscar SonidoNPC automáticamente si no está asignado
        if (sonidoNPC == null)
            sonidoNPC = GetComponentInChildren<SonidoNPC>();
        if (sonidoNPC == null && npcTransform != null)
            sonidoNPC = npcTransform.GetComponent<SonidoNPC>();
    }

    // ─────────────────────────────────────────────────────────────────────
    void AsignarBotones()
    {
        if (boton1 != null) { boton1.onClick.RemoveAllListeners(); boton1.onClick.AddListener(Choice1Void); }
        if (boton2 != null) { boton2.onClick.RemoveAllListeners(); boton2.onClick.AddListener(Choice2Void); }
        if (boton3 != null) { boton3.onClick.RemoveAllListeners(); boton3.onClick.AddListener(Choice3Void); }
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

                    if (gestorZonasPrevio != null && gestorZonasPrevio.PuzzlePendiente())
                    {
                        if (interactionText != null)
                            interactionText.text = "Pisa las zonas marcadas primero";
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

        yield return EscribirTexto("Yo: ", textoYo, false);
        yield return PresionarMouse();
        yield return EscribirTexto("SamuVR: ", textoNPC, true); // ← NPC habla

        yield return new WaitForSeconds(0.8f);

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

        yield return EscribirTexto("Yo: ", textoRespuesta, false);
        yield return new WaitForSeconds(0.5f);

        if (!string.IsNullOrEmpty(respuestaNPC))
        {
            yield return new WaitForSeconds(0.4f);
            yield return EscribirTexto("SamuVR: ", respuestaNPC, true); // ← NPC responde
            yield return new WaitForSeconds(1.5f);
        }
        else
        {
            yield return new WaitForSeconds(1.5f);
        }

        talkPanel.SetActive(false);
        talkText.SetActive(false);
        subText.text = "";

        // ── Animación de cámara (solo Momento 8) ──────────────────────────
        if (usarAnimacionMomento8 && animatorCamara != null)
        {
            if (firstPersonController != null)
                firstPersonController.enabled = false;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            animatorCamara.enabled = true;
            animatorCamara.SetTrigger(triggerAnimacion);

            // ── ▼ AUDIO: respiración durante la animación (NUEVO) ──────────
            if (AudioManager.Instance != null)
                AudioManager.Instance.IniciarAnimacionMomento8();
            // ── ▲ AUDIO ────────────────────────────────────────────────────

            yield return new WaitForSeconds(duracionAnimacion);

            // ── ▼ AUDIO: detener respiración (NUEVO) ───────────────────────
            if (AudioManager.Instance != null)
                AudioManager.Instance.TerminarAnimacionMomento8();
            // ── ▲ AUDIO ────────────────────────────────────────────────────

            animatorCamara.enabled = false;
        }

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
    // ── ▼ MODIFICADO: EscribirTexto ahora activa/desactiva voz del NPC ──
    IEnumerator EscribirTexto(string hablante, string mensaje, bool esNPC)
    {
        subText.text = hablante;

        if (esNPC && sonidoNPC != null)
            sonidoNPC.HablarNPC();

        foreach (char c in mensaje)
        {
            subText.text += c;
            yield return new WaitForSeconds(_time);
        }

        if (esNPC && sonidoNPC != null)
            sonidoNPC.PararVoz();
    }
    // ── ▲ FIN MODIFICACIÓN ──────────────────────────────────────────────

    IEnumerator PresionarMouse()
    {
        while (!Mouse.current.leftButton.wasPressedThisFrame)
            yield return null;
    }
}