using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

/// <summary>
/// Pantalla de retroalimentación final.
/// MODIFICADO: Inicia música de retroalimentación al mostrar el panel.
///             Añade sonidos a botones Skip y Reiniciar.
/// </summary>
public class UIRetroalimentacion : MonoBehaviour
{
    [Header("── Video ──────────────────────────────")]
    public VideoPlayer videoPlayer;
    public RawImage videoScreen;
    public GameObject botonSaltar;

    // ── ▼ NUEVO: el texto del botón Saltar se asigna por código, porque en el
    //     build no estaba apareciendo (problema típico de fuente/Text vacío) ──
    [Header("── Texto del botón Saltar ──────────────")]
    [Tooltip("Text (UI) del botón Saltar. Arrástralo aquí; el script le pone el texto en Start().")]
    public Text textoBotonSaltar;
    [Tooltip("Texto que se va a mostrar en el botón Saltar")]
    public string textoSaltar = "Saltar";
    // ── ▲ NUEVO ─────────────────────────────────────────────────────────────

    [Header("── Panel de retroalimentación ─────────")]
    public GameObject panelRetro;

    [Header("── ScrollRect del mapa (opcional) ───────")]
    [Tooltip("Si el mapa vive dentro de un ScrollRect, asígnalo aquí: este script se " +
             "asegurará de que quede SIEMPRE desactivado (el mapa es estático, sin " +
             "scroll ni arrastre — MapaDecisiones lo centra y escala por código).")]
    public ScrollRect scrollRectMapa;
    // ── ▼ MODIFICADO: ya no se usa — el mapa nunca tiene scroll horizontal.
    //     Se deja el campo (marcado [HideInInspector]) solo para no romper
    //     referencias existentes en el Inspector de otras escenas/prefabs.
    [HideInInspector]
    public bool scrollHorizontal = false;
    // ── ▲ MODIFICADO ────────────────────────────────────────────────────────

    // ── ▼ NUEVO: pantalla de reflexión, aparece ANTES de la retroalimentación ──
    [Header("── Reflexión (entre cinemática y retro) ─")]
    [Tooltip("Panel propio para los mensajes de reflexión, separado del panel de retro y del mapa")]
    public GameObject panelReflexion;
    [Tooltip("Texto donde se va mostrando cada mensaje de reflexión")]
    public Text textoReflexion;
    [Tooltip("CanvasGroup del panelReflexion, usado para el fundido. Se busca/crea automáticamente si está vacío.")]
    public CanvasGroup canvasGroupReflexion;
    [Tooltip("Mensajes de reflexión que se muestran uno por uno, en orden, antes de la retroalimentación")]
    [TextArea(2, 4)]
    public string[] mensajesReflexion;
    [Tooltip("Segundos que se mantiene visible cada mensaje (sin contar el fundido)")]
    public float duracionPorMensaje = 4f;
    [Tooltip("Duración del fundido de entrada/salida de cada mensaje")]
    public float duracionFadeReflexion = 0.8f;
    // ── ▲ NUEVO ─────────────────────────────────────────────────────────────

    [Header("── Textos de retroalimentación ────────")]
    public Text textoResumen;
    public Text textoConfianza;
    public Text textoRiesgo;
    public Text textoFinal;

    // ── ▼ NUEVO: video adicional entre la reflexión y la retroalimentación ──
    [Header("── Video final (entre reflexión y retro) ─")]
    [Tooltip("VideoPlayer opcional que se reproduce DESPUÉS de los mensajes de reflexión " +
             "y ANTES de mostrar la retroalimentación final. Déjalo vacío si no se usa.")]
    public VideoPlayer videoPlayerFinal;
    public RawImage videoScreenFinal;
    [Tooltip("Botón para saltar este video (opcional)")]
    public Button botonSaltarFinal;
    [Tooltip("Text (UI) del botón Saltar de este video. Se le pone el texto por código en Start().")]
    public Text textoBotonSaltarFinal;
    public string textoSaltarFinal = "Saltar";

    [Header("── Texto sobre el video final ────────────")]
    [Tooltip("Texto opcional que aparece SOBRE el segundo video (justo después de los mensajes " +
             "de reflexión) y se desvanece antes de pasar a la retroalimentación. " +
             "Déjalo sin asignar o vacío si no lo necesitas.")]
    public Text textoVideoFinal;
    [TextArea(2, 4)]
    public string mensajeVideoFinal = "";
    [Tooltip("Duración del fundido de entrada/salida de este texto")]
    public float duracionFadeTextoVideoFinal = 0.6f;
    [Tooltip("CanvasGroup del texto, usado para el fundido. Se busca/crea automáticamente si está vacío.")]
    public CanvasGroup canvasGroupTextoVideoFinal;

    [Header("── Fundido de salida (video final → retro) ─")]
    [Tooltip("Panel negro a pantalla completa (por encima del video) para no cortar abruptamente " +
             "del video final a la retroalimentación. Opcional: si no se asigna, se pasa directo sin fundido.")]
    public Image panelFadeSalida;
    [Tooltip("Duración del fundido a negro y de regreso")]
    public float duracionFadeSalida = 0.8f;
    // ── ▲ NUEVO ─────────────────────────────────────────────────────────────

    [Header("── Botones ─────────────────────────────")]
    public Button botonSaltarBtn;
    public Button botonReiniciarBtn;

    [Header("── Reinicio ────────────────────────────")]
    public string escenaInicio = "NIVEL1";

    [Header("── Menú principal ───────────────────────")]
    [Tooltip("Botón 'Ir al Menú'. Conéctalo en el Inspector (OnClick → IrAlMenu)")]
    public Button botonMenu;
    [Tooltip("Nombre exacto de la escena del menú de inicio")]
    public string escenaMenu = "Menu";

    // ── ▼ AUDIO (NUEVO) ──────────────────────────────────────────────────
    [Header("── Audio UI ────────────────────────────")]
    [Tooltip("SonidoUI del Canvas (se busca automáticamente)")]
    public SonidoUI sonidoUI;
    // ── ▲ AUDIO ──────────────────────────────────────────────────────────

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (panelRetro != null)
            panelRetro.SetActive(false);

        // ── ▼ NUEVO ──────────────────────────────────────────────────────
        if (panelReflexion != null)
            panelReflexion.SetActive(false);
        // ── ▲ NUEVO ──────────────────────────────────────────────────────

        if (botonSaltar != null)
            botonSaltar.SetActive(true);

        // ── ▼ NUEVO: forzar el texto del botón Saltar por código ───────────
        if (textoBotonSaltar == null && botonSaltar != null)
            textoBotonSaltar = botonSaltar.GetComponentInChildren<Text>(true);

        if (textoBotonSaltar != null)
            textoBotonSaltar.text = textoSaltar;
        // ── ▲ NUEVO ──────────────────────────────────────────────────────

        // ── ▼ NUEVO: preparar el botón Saltar del video final (opcional) ──
        if (botonSaltarFinal != null)
        {
            botonSaltarFinal.gameObject.SetActive(false);
            botonSaltarFinal.onClick.RemoveAllListeners();
            botonSaltarFinal.onClick.AddListener(SaltarVideoFinal);
        }

        if (textoBotonSaltarFinal == null && botonSaltarFinal != null)
            textoBotonSaltarFinal = botonSaltarFinal.GetComponentInChildren<Text>(true);

        if (textoBotonSaltarFinal != null)
            textoBotonSaltarFinal.text = textoSaltarFinal;
        // ── ▲ NUEVO ──────────────────────────────────────────────────────

        // ── ▼ NUEVO: el texto sobre el video final empieza oculto ────────
        if (textoVideoFinal != null)
            textoVideoFinal.gameObject.SetActive(false);
        // ── ▲ NUEVO ──────────────────────────────────────────────────────

        // ── ▼ NUEVO: el cursor debe estar libre durante toda esta escena
        //     (cinemática de finales, reflexión, video final y retroalimentación) ──
        GestorCursor.PedirLibre(this);
        // ── ▲ NUEVO ──────────────────────────────────────────────────────

        // Buscar SonidoUI
        if (sonidoUI == null)
            sonidoUI = FindAnyObjectByType<SonidoUI>();

        // ── ▼ AUDIO: registrar botones (NUEVO) ───────────────────────────
        if (sonidoUI != null)
        {
            if (botonSaltarBtn != null) sonidoUI.RegistrarBoton(botonSaltarBtn, SonidoUI.TipoSonidoBtn.Skip);
            if (botonReiniciarBtn != null) sonidoUI.RegistrarBoton(botonReiniciarBtn, SonidoUI.TipoSonidoBtn.Reiniciar);
            if (botonMenu != null) sonidoUI.RegistrarBoton(botonMenu, SonidoUI.TipoSonidoBtn.Skip);
            if (botonSaltarFinal != null) sonidoUI.RegistrarBoton(botonSaltarFinal, SonidoUI.TipoSonidoBtn.Skip);
        }
        // ── ▲ AUDIO ──────────────────────────────────────────────────────

        // Registrar listener del botón Menú
        if (botonMenu != null)
            botonMenu.onClick.AddListener(IrAlMenu);

        // Detener música de juego/tensión al entrar a la escena de final
        // (la música de retro empezará al terminar el video)
        if (AudioManager.Instance != null)
            AudioManager.Instance.DetenerMusicaJuego();

        StartCoroutine(ReproducirVideoCO());
    }

    // Flag interno: true mientras el video está reproduciéndose
    bool _videoActivo = false;

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator ReproducirVideoCO()
    {
        if (videoPlayer == null || videoScreen == null)
        {
            Debug.LogWarning("[UIRetroalimentacion] Falta VideoPlayer o RawImage. Se salta el video.");
            yield return SecuenciaReflexionYRetroCO();
            yield break;
        }

        videoScreen.gameObject.SetActive(true);
        _videoActivo = true;

        videoPlayer.Prepare();
        yield return new WaitUntil(() => videoPlayer.isPrepared);

        videoPlayer.Play();

        yield return new WaitUntil(() =>
            !videoPlayer.isPlaying ||
            (videoPlayer.frameCount > 0 &&
             videoPlayer.frame >= (long)videoPlayer.frameCount - 2)
        );

        _videoActivo = false;
        yield return SecuenciaReflexionYRetroCO();
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Solo debe llamarse desde el botón Saltar.
    /// El flag _videoActivo impide que cualquier otro código lo llame accidentalmente.
    /// </summary>
    public void SaltarVideo()
    {
        // Si el video ya terminó por sí solo, no hacer nada
        if (!_videoActivo) return;

        SonidoUI.TocarSkip();

        _videoActivo = false;

        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Stop();

        StopAllCoroutines();
        StartCoroutine(SecuenciaReflexionYRetroCO());
    }

    // ─────────────────────────────────────────────────────────────────────
    // ── ▼ NUEVO: oculta video/skip, muestra los mensajes de reflexión
    //     (pantalla propia, sin el mapa) y luego pasa a la retroalimentación ──
    IEnumerator SecuenciaReflexionYRetroCO()
    {
        if (videoScreen != null)
            videoScreen.gameObject.SetActive(false);

        if (botonSaltar != null)
            botonSaltar.SetActive(false);

        yield return MostrarReflexionCO();

        // ── ▼ NUEVO: espacio para un segundo video, justo después del mensaje
        //     de reflexión y ANTES de mostrar la retroalimentación final.
        //     Si no se asigna videoPlayerFinal, este paso se salta solo. ──
        yield return ReproducirVideoFinalCO();
        // ── ▲ NUEVO ─────────────────────────────────────────────────────────

        MostrarPantallaRetro();

        // ── ▼ NUEVO: si hubo fundido a negro, regresar de negro ya con la
        //     retroalimentación visible, para que la transición sea suave ──
        yield return FundirImagenCO(panelFadeSalida, 1f, 0f, duracionFadeSalida);
        // ── ▲ NUEVO ─────────────────────────────────────────────────────────
    }

    // ─────────────────────────────────────────────────────────────────────
    // ── ▼ NUEVO: video opcional entre la reflexión y la retroalimentación ──
    bool _videoFinalActivo = false;

    IEnumerator ReproducirVideoFinalCO()
    {
        if (videoPlayerFinal == null || videoScreenFinal == null)
            yield break; // No hay video final configurado, se pasa directo a la retro

        videoScreenFinal.gameObject.SetActive(true);
        if (botonSaltarFinal != null) botonSaltarFinal.gameObject.SetActive(true);
        _videoFinalActivo = true;

        videoPlayerFinal.Prepare();
        yield return new WaitUntil(() => videoPlayerFinal.isPrepared);

        videoPlayerFinal.Play();

        // ── NUEVO: mostrar el texto sobre el video, con su propio fundido ──
        CanvasGroup cgTexto = null;
        if (textoVideoFinal != null && !string.IsNullOrEmpty(mensajeVideoFinal))
        {
            textoVideoFinal.text = mensajeVideoFinal;
            textoVideoFinal.gameObject.SetActive(true);

            cgTexto = canvasGroupTextoVideoFinal;
            if (cgTexto == null) cgTexto = textoVideoFinal.GetComponent<CanvasGroup>();
            if (cgTexto == null) cgTexto = textoVideoFinal.gameObject.AddComponent<CanvasGroup>();

            yield return FundirCanvasGroupCO(cgTexto, 0f, 1f, duracionFadeTextoVideoFinal);
        }

        yield return new WaitUntil(() =>
            !_videoFinalActivo ||
            !videoPlayerFinal.isPlaying ||
            (videoPlayerFinal.frameCount > 0 &&
             videoPlayerFinal.frame >= (long)videoPlayerFinal.frameCount - 2)
        );

        _videoFinalActivo = false;

        // ── NUEVO: ocultar el texto ANTES de cortar a la retroalimentación ──
        if (cgTexto != null)
        {
            yield return FundirCanvasGroupCO(cgTexto, 1f, 0f, duracionFadeTextoVideoFinal);
            textoVideoFinal.gameObject.SetActive(false);
        }

        if (botonSaltarFinal != null) botonSaltarFinal.gameObject.SetActive(false);

        // ── NUEVO: fundido a negro para no cortar abruptamente a la retro ──
        yield return FundirImagenCO(panelFadeSalida, 0f, 1f, duracionFadeSalida);

        if (videoPlayerFinal.isPlaying)
            videoPlayerFinal.Stop();

        videoScreenFinal.gameObject.SetActive(false);
    }

    /// <summary>
    /// Solo debe llamarse desde el botón Saltar del video final.
    /// El flag _videoFinalActivo impide que se llame accidentalmente cuando no aplica.
    /// </summary>
    public void SaltarVideoFinal()
    {
        if (!_videoFinalActivo) return;
        SonidoUI.TocarSkip();
        _videoFinalActivo = false;
    }

    // ── NUEVO: fundido a negro de un Image a pantalla completa (opcional) ──
    IEnumerator FundirImagenCO(Image img, float desde, float hasta, float duracion)
    {
        if (img == null) yield break;

        float t = 0f;
        Color c = img.color;
        c.a = desde;
        img.color = c;

        while (t < duracion)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(desde, hasta, t / duracion);
            img.color = c;
            yield return null;
        }

        c.a = hasta;
        img.color = c;
    }
    // ── ▲ NUEVO ─────────────────────────────────────────────────────────────

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator MostrarReflexionCO()
    {
        if (panelReflexion == null || textoReflexion == null || mensajesReflexion == null || mensajesReflexion.Length == 0)
            yield break;

        CanvasGroup cg = canvasGroupReflexion;
        if (cg == null) cg = panelReflexion.GetComponent<CanvasGroup>();
        if (cg == null) cg = panelReflexion.AddComponent<CanvasGroup>();

        panelReflexion.SetActive(true);

        foreach (string mensaje in mensajesReflexion)
        {
            if (string.IsNullOrEmpty(mensaje)) continue;

            textoReflexion.text = mensaje;

            yield return FundirCanvasGroupCO(cg, 0f, 1f, duracionFadeReflexion);
            yield return new WaitForSeconds(duracionPorMensaje);
            yield return FundirCanvasGroupCO(cg, 1f, 0f, duracionFadeReflexion);
        }

        panelReflexion.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator FundirCanvasGroupCO(CanvasGroup cg, float desde, float hasta, float duracion)
    {
        float t = 0f;
        cg.alpha = desde;
        while (t < duracion)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(desde, hasta, t / duracion);
            yield return null;
        }
        cg.alpha = hasta;
    }
    // ── ▲ NUEVO ─────────────────────────────────────────────────────────────

    // ─────────────────────────────────────────────────────────────────────
    void MostrarPantallaRetro()
    {
        if (videoScreen != null)
            videoScreen.gameObject.SetActive(false);

        if (botonSaltar != null)
            botonSaltar.SetActive(false);

        if (panelRetro != null)
            panelRetro.SetActive(true);

        // ── ▼ MODIFICADO: el mapa ahora es 100% estático (sin scroll ni
        //     arrastre) y se centra/escala solo, por código, en
        //     MapaDecisiones. ANTES, esta línea volvía a poner
        //     "horizontal = true" justo al mostrar el panel, lo que
        //     re-habilitaba el arrastre por unos frames antes de que
        //     MapaDecisiones lo apagara de nuevo — por eso a veces "se movía
        //     solo" o quedaba descuadrado. Ahora simplemente lo forzamos
        //     apagado, sin excepción. ──
        if (scrollRectMapa != null)
        {
            scrollRectMapa.horizontal = false;
            scrollRectMapa.vertical = false;
            scrollRectMapa.enabled = false;
        }
        // ── ▲ MODIFICADO ────────────────────────────────────────────────────────

        // ── ▼ AUDIO: iniciar música de retroalimentación (NUEVO) ─────────
        if (AudioManager.Instance != null)
            AudioManager.Instance.IniciarMusicaRetro();
        // ── ▲ AUDIO ──────────────────────────────────────────────────────

        RellenarTextos();
    }

    // ─────────────────────────────────────────────────────────────────────
    void RellenarTextos()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[UIRetroalimentacion] GameManager no encontrado.");
            return;
        }

        GameManager gm = GameManager.Instance;

        if (textoResumen != null) textoResumen.text = gm.ObtenerResumen();
        if (textoConfianza != null) textoConfianza.text = $"Confianza: {gm.PuntosConfianza} pts";
        if (textoRiesgo != null) textoRiesgo.text = $"Riesgo:    {gm.PuntosRiesgo} pts";

        if (textoFinal != null)
            textoFinal.text = $"{gm.ObtenerTituloFinal()}\n{gm.ObtenerMensajeFinal()}";
    }

    // ─────────────────────────────────────────────────────────────────────
    public void ReiniciarExperiencia()
    {
        // ── ▼ AUDIO: sonido reiniciar (NUEVO) ────────────────────────────
        SonidoUI.TocarReiniciar();
        // ── ▲ AUDIO ──────────────────────────────────────────────────────

        // ── ▼ NUEVO: liberar el pedido de cursor libre antes de salir ────
        GestorCursor.Liberar(this);
        // ── ▲ NUEVO ──────────────────────────────────────────────────────

        if (GameManager.Instance != null)
            GameManager.Instance.Reiniciar();

        SceneManager.LoadScene(escenaInicio);
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Vuelve al menú principal sin reiniciar el GameManager.
    /// Asigna al botón "Ir al Menú" en el Inspector (OnClick → IrAlMenu).
    /// </summary>
    public void IrAlMenu()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.DetenerMusicaJuego();

        // ── ▼ NUEVO: liberar el pedido de cursor libre antes de salir ────
        GestorCursor.Liberar(this);
        // ── ▲ NUEVO ──────────────────────────────────────────────────────

        if (GameManager.Instance != null)
            GameManager.Instance.Reiniciar();

        SceneManager.LoadScene(escenaMenu);
    }

    // ─────────────────────────────────────────────────────────────────────
    // ── ▼ NUEVO: red de seguridad — si el objeto se destruye por cualquier
    //     otro motivo (recarga de escena, etc.), liberar el pedido de cursor
    //     para que no quede "atascado" pidiéndolo desde una instancia muerta ──
    void OnDestroy()
    {
        GestorCursor.Liberar(this);
    }
    // ── ▲ NUEVO ─────────────────────────────────────────────────────────────
}
