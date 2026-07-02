using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// Menú de pausa.
/// El control de volumen fue eliminado — todo el audio se gestiona
/// exclusivamente a través de AudioManager y su AudioMixer.
/// </summary>
public class MenuPausa : MonoBehaviour
{
    [Header("── Escenas ──────────────────────────────")]
    public string escenaMenu = "MenuInicio";

    [Header("── Panels ───────────────────────────────")]
    public GameObject panelPausa;
    public GameObject panelBotones;

    [Header("── Botones ─────────────────────────────")]
    public Button botonContinuar;
    public Button botonSalir;

    [Header("── Fade ─────────────────────────────────")]
    public Image panelFade;
    public float duracionFade = 0.8f;

    [Header("── Jugador ──────────────────────────────")]
    public MonoBehaviour firstPersonController;

    [Header("── Audio UI ────────────────────────────")]
    public SonidoUI sonidoUI;

    [Header("── Orden de Canvas ─────────────────────")]
    [Tooltip("Canvas que contiene el panel de pausa. Se busca automáticamente si se deja vacío. " +
             "Se fuerza a estar SIEMPRE por encima de cualquier otro Canvas (celular, objetivos, etc.)")]
    public Canvas canvasPausa;

    [Tooltip("Sort Order que se le va a forzar al Canvas de pausa. Debe ser MAYOR " +
             "que el de cualquier otro Canvas del juego (celular, objetivo, diálogos, etc.)")]
    public int sortOrderPausa = 999;

    bool _pausado = false;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (panelPausa != null) panelPausa.SetActive(false);
        if (panelFade != null) SetAlpha(0f);

        if (sonidoUI == null)
            sonidoUI = FindAnyObjectByType<SonidoUI>();

        if (sonidoUI != null)
        {
            if (botonContinuar != null) sonidoUI.RegistrarBoton(botonContinuar, SonidoUI.TipoSonidoBtn.Click);
            if (botonSalir != null) sonidoUI.RegistrarBoton(botonSalir, SonidoUI.TipoSonidoBtn.Click);
        }

        // ── ▼ Asegurar Canvas y EventSystem correctos ─────────────────────
        if (canvasPausa == null && panelPausa != null)
            canvasPausa = panelPausa.GetComponentInParent<Canvas>();

        if (canvasPausa != null)
        {
            canvasPausa.overrideSorting = true;
            canvasPausa.sortingOrder = sortOrderPausa;

            // Asegurar que el Canvas tenga GraphicRaycaster, si no, los botones no reciben clics
            if (canvasPausa.GetComponent<GraphicRaycaster>() == null)
                canvasPausa.gameObject.AddComponent<GraphicRaycaster>();
        }
        else
        {
            Debug.LogWarning("[MenuPausa] No se encontró un Canvas para panelPausa. " +
                              "Asigná 'canvasPausa' manualmente en el Inspector.");
        }

        // Advertir si hay más de un EventSystem en la escena (causa muy común
        // de que los botones dejen de responder de un día para el otro)
        var eventSystems = FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Exclude);
        if (eventSystems.Length > 1)
            Debug.LogWarning("[MenuPausa] Hay " + eventSystems.Length + " EventSystem en la escena. " +
                              "Debe haber solo UNO o los botones fallan de forma intermitente.");
        else if (eventSystems.Length == 0)
            Debug.LogWarning("[MenuPausa] No hay ningún EventSystem en la escena. " +
                              "Los botones NO van a funcionar sin uno (GameObject > UI > Event System).");
        // ── ▲ ──────────────────────────────────────────────────────────────
    }

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (_pausado) Continuar();
            else Pausar();
        }

        // ── ▼ DIAGNÓSTICO: mientras el menú está pausado, cada clic imprime
        //     en consola qué GameObject recibió realmente el raycast. Si al
        //     clickear un botón el log muestra OTRO objeto (o ninguno), ese
        //     es el que está tapando el botón. Borrar este bloque una vez
        //     resuelto el problema. ──────────────────────────────────────
        if (_pausado && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current != null)
            {
                PointerEventData datosPuntero = new PointerEventData(EventSystem.current)
                {
                    position = Mouse.current.position.ReadValue()
                };
                List<RaycastResult> resultados = new List<RaycastResult>();
                EventSystem.current.RaycastAll(datosPuntero, resultados);

                if (resultados.Count == 0)
                {
                    Debug.Log("[MenuPausa][DIAGNÓSTICO] El clic no golpeó NINGÚN elemento de UI. " +
                              "Revisá si hay un GraphicRaycaster en el Canvas correcto.");
                }
                else
                {
                    Debug.Log("[MenuPausa][DIAGNÓSTICO] El clic golpeó (de arriba a abajo): " +
                              string.Join(" → ", resultados.ConvertAll(r => r.gameObject.name)));
                }
            }
            else
            {
                Debug.LogWarning("[MenuPausa][DIAGNÓSTICO] No hay EventSystem en la escena.");
            }
        }
        // ── ▲ DIAGNÓSTICO ────────────────────────────────────────────────
    }

    // ─────────────────────────────────────────────────────────────────────
    void Pausar()
    {
        _pausado = true;
        Time.timeScale = 0f;

        if (firstPersonController != null) firstPersonController.enabled = false;

        GestorCursor.PedirLibre(this);

        // Reforzar que el canvas de pausa quede arriba de todo, incluso si
        // otro sistema (celular, objetivo, etc.) cambió el sorting mientras jugabas
        if (canvasPausa != null)
        {
            canvasPausa.overrideSorting = true;
            canvasPausa.sortingOrder = sortOrderPausa;
        }
        if (panelPausa != null) panelPausa.transform.SetAsLastSibling();

        // ── ▼ Forzar que nada bloquee la interacción con los botones ───────
        // Si algún CanvasGroup (por ejemplo de un sistema de fade) quedó con
        // Interactable o Blocks Raycasts en false, los botones se ven pero
        // NUNCA se resaltan ni reciben clics, sin ningún error en consola.
        if (panelPausa != null)
        {
            foreach (CanvasGroup cg in panelPausa.GetComponentsInChildren<CanvasGroup>(true))
            {
                cg.interactable = true;
                cg.blocksRaycasts = true;
                cg.alpha = 1f;
            }
        }

        if (botonContinuar != null) botonContinuar.interactable = true;
        if (botonSalir != null) botonSalir.interactable = true;
        // ── ▲ ────────────────────────────────────────────────────────────

        if (panelPausa != null) panelPausa.SetActive(true);
        if (panelBotones != null) panelBotones.SetActive(true);
    }

    // ─────────────────────────────────────────────────────────────────────
    public void Continuar()
    {
        SonidoUI.TocarClick();

        _pausado = false;
        Time.timeScale = 1f;

        if (panelPausa != null) panelPausa.SetActive(false);

        GestorCursor.Liberar(this);

        // Solo recuperar el control del jugador (movimiento + cursor bloqueado)
        // si NINGÚN otro sistema sigue necesitando el cursor libre. Ej: si
        // pausaste en medio de un diálogo o del celular, esos siguen abiertos
        // debajo y deben conservar la prioridad sobre el cursor.
        if (!GestorCursor.CursorRequeridoLibre)
        {
            if (firstPersonController != null) firstPersonController.enabled = true;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    public void Salir()
    {
        SonidoUI.TocarClick();
        StartCoroutine(SalirCO());
    }

    IEnumerator SalirCO()
    {
        Time.timeScale = 1f;
        yield return Fade(0f, 1f, duracionFade);

        if (GameManager.Instance != null)
            GameManager.Instance.Reiniciar();

        GestorCursor.PedirLibre(this); // pantalla de menú → cursor siempre libre

        SceneManager.LoadScene(escenaMenu);
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator Fade(float desde, float hasta, float duracion)
    {
        if (panelFade == null) yield break;
        float t = 0f;
        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(desde, hasta, t / duracion));
            yield return null;
        }
        SetAlpha(hasta);
    }

    void SetAlpha(float a)
    {
        if (panelFade == null) return;
        Color c = panelFade.color;
        c.a = a;
        panelFade.color = c;

        // Si el fade está invisible (alpha 0), que no bloquee clics de la UI
        // que esté debajo. Si está visible (fundiendo a negro), sí debe bloquear.
        panelFade.raycastTarget = a > 0.01f;
    }
}