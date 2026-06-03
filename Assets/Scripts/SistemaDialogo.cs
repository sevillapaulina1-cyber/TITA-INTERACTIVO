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

    [Header("── Zoom Out + Rotación (solo Momento 8) ──")]
    public bool hacerZoomOut = false;
    public Camera camara;
    public float fovInicial = 60f;
    public float fovFinal = 90f;
    public float duracionZoom = 2.0f;
    public float esperaZoom = 1.5f;
    [Tooltip("Cuántos grados rota la cámara a cada lado durante el barrido")]
    public float anguloBarrido = 60f;
    [Tooltip("Cuántos grados sube y baja la cámara durante el barrido")]
    public float anguloVertical = 20f;
    [Tooltip("Duración total del barrido izquierda-derecha-centro")]
    public float duracionBarrido = 4.0f;

    bool _puedeInteractuar = true;
    float _time = 0.05f;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (npcTransform != null && posicionNPCEsteDia != null)
            npcTransform.position = posicionNPCEsteDia.position;
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

        yield return EscribirTexto("Yo: ", textoYo);
        yield return PresionarMouse();
        yield return EscribirTexto("Kid: ", textoNPC);

        yield return new WaitForSeconds(0.8f);

        if (hacerZoomOut && camara != null)
        {
            yield return ZoomOutCO();
            yield return new WaitForSeconds(esperaZoom);
        }

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
        Quaternion rotacionOriginal = camara.transform.localRotation;

        // ── Paso 1: Zoom out del FOV ──────────────────────────────────────
        float t = 0f;
        camara.fieldOfView = fovInicial;
        while (t < duracionZoom)
        {
            t += Time.deltaTime;
            camara.fieldOfView = Mathf.Lerp(fovInicial, fovFinal, t / duracionZoom);
            yield return null;
        }
        camara.fieldOfView = fovFinal;

        // ── Paso 2: Barrido en 4 tramos ───────────────────────────────────
        // Tramo 1: centro → izquierda + arriba
        // Tramo 2: izquierda+arriba → derecha + abajo
        // Tramo 3: derecha+abajo → izquierda + centro vertical
        // Tramo 4: izquierda → centro (posición original)

        float tramo = duracionBarrido / 4f;

        Quaternion izqArriba = rotacionOriginal * Quaternion.Euler(-anguloVertical, -anguloBarrido, 0f);
        Quaternion derAbajo = rotacionOriginal * Quaternion.Euler(anguloVertical, anguloBarrido, 0f);
        Quaternion izqCentro = rotacionOriginal * Quaternion.Euler(0f, -anguloBarrido * 0.5f, 0f);

        // Tramo 1: centro → izquierda arriba
        yield return LerpRotacion(rotacionOriginal, izqArriba, tramo);

        // Tramo 2: izquierda arriba → derecha abajo
        yield return LerpRotacion(izqArriba, derAbajo, tramo * 2f);

        // Tramo 3: derecha abajo → izquierda centro
        yield return LerpRotacion(derAbajo, izqCentro, tramo);

        // Tramo 4: izquierda centro → rotación original
        yield return LerpRotacion(izqCentro, rotacionOriginal, tramo);

        camara.transform.localRotation = rotacionOriginal;
    }

    IEnumerator LerpRotacion(Quaternion desde, Quaternion hasta, float duracion)
    {
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            camara.transform.localRotation = Quaternion.Lerp(desde, hasta, t / duracion);
            yield return null;
        }
    }

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