using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mapa CONCEPTUAL de decisiones para la pantalla de retroalimentación.
/// Estilo: un "hub" por cada día (Día 1, Día 2, Día 3, Día 4), con sus 3 momentos
/// de ese día ramificados hacia la derecha (como un mapa mental). Los días están
/// apilados verticalmente y conectados entre sí por una línea vertical ("columna
/// del tiempo"). Al final se agrega una tarjeta con el resumen de puntos y el
/// mensaje explicando el desenlace (Final 1 / Final 2).
///
/// SETUP EN UNITY:
///   1. Crea un GameObject vacío en la escena de final → Add Component → MapaDecisiones
///   2. En el Canvas crea un panel "PanelMapa" → dentro de él, el RectTransform
///      "contenedor" donde se dibuja todo el mapa.
///   3. IMPORTANTE: el mapa ahora es bastante alto (4 días + tarjeta final), así que
///      mete "contenedor" dentro de un ScrollRect VERTICAL (Content = contenedor,
///      Viewport = el tamaño de pantalla). Así el jugador puede desplazarse por
///      todos los días. Si prefieres que quepa todo sin scroll, reduce
///      separacionEntreDias / anchoTarjeta / altoTarjeta desde el Inspector.
///   4. Si quieres que la leyenda quede siempre visible (sin moverse con el scroll),
///      sácala del ScrollRect y ponla en un panel fijo aparte.
///   5. prefabNodo debe ser un rectángulo redondeado (Image) con un Text hijo
///      cualquiera; el script crea sus propios textos de Título y Cuerpo encima,
///      así que ese Text hijo original se desactiva automáticamente.
///
/// COLORES:
///   Verde  → decisión de confianza
///   Rojo   → decisión riesgosa
///   Gris   → decisión neutra
///   colorDia → color del "hub" de cada día (distinto a las decisiones)
/// </summary>
public class MapaDecisiones : MonoBehaviour
{
    [Header("── Contenedor ──────────────────────────")]
    public RectTransform contenedor;    // Panel donde se dibuja el mapa (idealmente dentro de un ScrollRect vertical)

    [Header("── Prefabs ─────────────────────────────")]
    public GameObject prefabNodo;       // Rectángulo redondeado (Image) usado para hubs y tarjetas
    public GameObject prefabLinea;      // Image usada como línea conectora

    [Header("── Colores de decisión ─────────────────")]
    public Color colorVerde = new Color(0.11f, 0.73f, 0.33f);
    public Color colorRojo = new Color(0.85f, 0.15f, 0.15f);
    public Color colorGris = new Color(0.6f, 0.6f, 0.6f);
    public Color colorLinea = new Color(1f, 1f, 1f, 0.35f);

    [Header("── Color del hub de cada día ────────────")]
    public Color colorDia = new Color(0.14f, 0.42f, 0.48f);

    [Header("── Tamaños de tarjetas ─────────────────")]
    [Tooltip("Ancho/alto del hub de cada día (la cajita 'Día N')")]
    public float anchoHubDia = 150f;
    public float altoHubDia = 80f;
    [Tooltip("Ancho/alto de cada tarjeta de momento")]
    public float anchoTarjeta = 280f;
    public float altoTarjeta = 170f;
    [Tooltip("Ancho/alto de la tarjeta final de resumen")]
    public float anchoTarjetaFinal = 360f;
    public float altoTarjetaFinal = 230f;

    [Header("── Distribución (espaciado) ────────────")]
    [Tooltip("Separación horizontal entre el hub del día y sus tarjetas de momento")]
    public float distanciaHubATarjetas = 360f;
    [Tooltip("Separación vertical entre las 3 tarjetas de un mismo día")]
    public float separacionVerticalMomentos = 200f;
    [Tooltip("Separación vertical entre el hub de un día y el del siguiente")]
    public float separacionEntreDias = 640f;
    [Tooltip("Grosor de las líneas conectoras")]
    public float grosorLinea = 4f;

    [Header("── Texto dentro de las tarjetas ─────────")]
    [Tooltip("Muestra el texto exacto que el jugador eligió, dentro de cada tarjeta de momento")]
    public bool mostrarTextoEleccion = true;
    [Tooltip("Espacio (padding) entre el borde de la tarjeta y el texto, para que no se vea apretado")]
    public float paddingInternoTarjeta = 20f;
    [Tooltip("Porción de la tarjeta (0 a 1) reservada para el título cuando hay cuerpo de texto")]
    [Range(0.15f, 0.5f)]
    public float alturaRelativaTitulo = 0.3f;
    [Tooltip("Espacio extra (0 a 1) entre el título y el cuerpo, para separarlos bien")]
    [Range(0f, 0.15f)]
    public float separacionTituloCuerpo = 0.06f;
    public int tamanoFuenteTitulo = 17;
    public int tamanoFuenteEleccion = 14;
    public Color colorTitulo = Color.white;
    public Color colorTextoEleccion = new Color(1f, 1f, 1f, 0.95f);

    [Header("── Nombres de momentos (opcional) ──────")]
    [Tooltip("12 nombres cortos para cada momento. Déjalos vacíos para usar números.")]
    public string[] nombresMomentos = {
        "Contacto", "Juego", "Cierre",
        "Reencuentro", "Emoción", "Vínculo",
        "Rutina", "Contexto", "Confianza",
        "Canal", "Secreto", "Encuentro"
    };

    [Header("── Nombres de los días (opcional) ──────")]
    public string[] nombresDias = { "Día 1", "Día 2", "Día 3", "Día 4" };

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[MapaDecisiones] GameManager no encontrado.");
            return;
        }

        GenerarMapa();
    }

    // ─────────────────────────────────────────────────────────────────────
    void GenerarMapa()
    {
        GameManager gm = GameManager.Instance;
        TipoEleccion[] historial = gm.HistorialElecciones;
        string[] textos = gm.HistorialTextos;

        float xHub = 0f;
        float xTarjetas = xHub + distanciaHubATarjetas;

        for (int d = 0; d < GameManager.TOTAL_DIAS; d++)
        {
            float yHub = -d * separacionEntreDias;

            string nombreDia = (nombresDias != null && d < nombresDias.Length && !string.IsNullOrEmpty(nombresDias[d]))
                ? nombresDias[d]
                : $"Día {d + 1}";

            CrearTarjeta(new Vector2(xHub, yHub), anchoHubDia, altoHubDia, colorDia, nombreDia, null);

            // ── Conectar con el hub del día anterior (columna del tiempo) ──
            if (d > 0)
            {
                float yHubAnterior = -(d - 1) * separacionEntreDias;
                CrearLinea(
                    new Vector2(xHub, yHubAnterior - altoHubDia * 0.5f),
                    new Vector2(xHub, yHub + altoHubDia * 0.5f));
            }

            // ── Las 3 tarjetas de momento de este día, ramificadas a la derecha ──
            for (int m = 0; m < GameManager.DECISIONES_POR_DIA; m++)
            {
                int i = d * GameManager.DECISIONES_POR_DIA + m;
                if (i >= GameManager.TOTAL_MOMENTOS) break;

                // m=0 → arriba del hub, m=1 → a la altura del hub, m=2 → abajo del hub
                float yMomento = yHub + (1 - m) * separacionVerticalMomentos;
                Vector2 posMomento = new Vector2(xTarjetas, yMomento);

                string nombreMomento = (nombresMomentos != null && i < nombresMomentos.Length && !string.IsNullOrEmpty(nombresMomentos[i]))
                    ? nombresMomentos[i]
                    : $"Momento {i + 1}";

                string textoElegido = (textos != null && i < textos.Length) ? textos[i] : "";

                CrearTarjeta(posMomento, anchoTarjeta, altoTarjeta, ColorSegunEleccion(historial[i]), nombreMomento, textoElegido);

                CrearLinea(
                    new Vector2(xHub + anchoHubDia * 0.5f, yHub),
                    new Vector2(posMomento.x - anchoTarjeta * 0.5f, posMomento.y));
            }
        }

        // ── Tarjeta final con el resumen de puntos y el mensaje del desenlace ──
        float yUltimoHub = -(GameManager.TOTAL_DIAS - 1) * separacionEntreDias;
        float yFinal = yUltimoHub - separacionEntreDias;

        CrearLinea(
            new Vector2(xHub, yUltimoHub - altoHubDia * 0.5f),
            new Vector2(xHub, yFinal + altoTarjetaFinal * 0.5f));

        CrearTarjetaFinal(new Vector2(xHub, yFinal));

        // ── Leyenda ──────────────────────────────────────────────────────
        CrearLeyenda();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Tarjeta con el resumen de puntos + el mensaje según el final obtenido.
    void CrearTarjetaFinal(Vector2 pos)
    {
        GameManager gm = GameManager.Instance;

        string titulo = gm.ObtenerTituloFinal();
        string cuerpo = $"Confianza: {gm.PuntosConfianza} pts    Riesgo: {gm.PuntosRiesgo} pts\n\n{gm.ObtenerMensajeFinal()}";

        Color colorFondo = gm.EsFinal1 ? colorVerde : colorRojo;

        CrearTarjeta(pos, anchoTarjetaFinal, altoTarjetaFinal, colorFondo, titulo, cuerpo);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Crea una tarjeta (hub de día, momento, o resumen final) con título arriba
    // y, si se le da texto de cuerpo, un bloque de texto separado debajo.
    GameObject CrearTarjeta(Vector2 pos, float ancho, float alto, Color colorFondo, string titulo, string cuerpo)
    {
        GameObject tarjeta = Instantiate(prefabNodo, contenedor);
        RectTransform rt = tarjeta.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(ancho, alto);

        Image img = tarjeta.GetComponent<Image>();
        if (img != null) img.color = colorFondo;

        // El prefab puede traer su propio Text; lo desactivamos porque el título
        // y el cuerpo se crean aparte, con su propio tamaño y separación.
        Text textoPrefab = tarjeta.GetComponentInChildren<Text>();
        if (textoPrefab != null) textoPrefab.gameObject.SetActive(false);

        bool tieneCuerpo = mostrarTextoEleccion && !string.IsNullOrEmpty(cuerpo);

        Vector2 anchorMinTitulo = tieneCuerpo ? new Vector2(0f, 1f - alturaRelativaTitulo) : Vector2.zero;
        CrearTextoHijo(rt, "Titulo", anchorMinTitulo, Vector2.one, paddingInternoTarjeta,
            titulo, tamanoFuenteTitulo, colorTitulo, FontStyle.Bold, TextAnchor.MiddleCenter, false);

        if (tieneCuerpo)
        {
            float topCuerpo = 1f - alturaRelativaTitulo - separacionTituloCuerpo;
            CrearTextoHijo(rt, "Cuerpo", new Vector2(0f, 0f), new Vector2(1f, topCuerpo), paddingInternoTarjeta,
                cuerpo, tamanoFuenteEleccion, colorTextoEleccion, FontStyle.Normal, TextAnchor.UpperCenter, true);
        }

        return tarjeta;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Crea un Text hijo dentro de una región anclada (anchorMin/anchorMax) de la
    // tarjeta, con padding interno para que el texto nunca toque el borde.
    void CrearTextoHijo(RectTransform padre, string nombre, Vector2 anchorMin, Vector2 anchorMax,
                         float padding, string texto, int fontSize, Color color,
                         FontStyle estilo, TextAnchor alineacion, bool conSaltoDeLinea)
    {
        GameObject obj = new GameObject(nombre);
        obj.transform.SetParent(padre, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(padding, padding);
        rt.offsetMax = new Vector2(-padding, -padding);

        Text txt = obj.AddComponent<Text>();
        txt.text = texto;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.fontStyle = estilo;
        txt.color = color;
        txt.alignment = alineacion;
        txt.horizontalOverflow = conSaltoDeLinea ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
    }

    // ─────────────────────────────────────────────────────────────────────
    void CrearLinea(Vector2 desde, Vector2 hasta)
    {
        if (prefabLinea == null) return;

        GameObject linea = Instantiate(prefabLinea, contenedor);
        RectTransform rt = linea.GetComponent<RectTransform>();

        Vector2 direccion = hasta - desde;
        float distancia = direccion.magnitude;
        float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;

        rt.anchoredPosition = desde + direccion * 0.5f;
        rt.sizeDelta = new Vector2(distancia, grosorLinea);
        rt.localRotation = Quaternion.Euler(0, 0, angulo);

        Image imgLinea = linea.GetComponent<Image>();
        if (imgLinea != null) imgLinea.color = colorLinea;

        // Mandar la línea detrás de las tarjetas
        linea.transform.SetAsFirstSibling();
    }

    // ─────────────────────────────────────────────────────────────────────
    void CrearLeyenda()
    {
        // Crea 3 entradas de leyenda en la esquina inferior izquierda del contenedor.
        // Nota: si "contenedor" está dentro de un ScrollRect, esta leyenda se moverá
        // junto con el contenido. Si quieres que quede fija, sácala a un panel aparte.
        string[] labels = { "Decisión de confianza", "Decisión neutra", "Decisión riesgosa" };
        Color[] colors = { colorVerde, colorGris, colorRojo };

        for (int i = 0; i < 3; i++)
        {
            GameObject punto = new GameObject($"LeyendaPunto{i}");
            punto.transform.SetParent(contenedor, false);
            RectTransform rtP = punto.AddComponent<RectTransform>();
            rtP.anchorMin = new Vector2(0, 0);
            rtP.anchorMax = new Vector2(0, 0);
            rtP.pivot = new Vector2(0, 0);
            rtP.anchoredPosition = new Vector2(20f, 20f + i * 26f);
            rtP.sizeDelta = new Vector2(14f, 14f);
            Image imgP = punto.AddComponent<Image>();
            imgP.color = colors[i];

            GameObject textoObj = new GameObject($"LeyendaTexto{i}");
            textoObj.transform.SetParent(contenedor, false);
            RectTransform rtT = textoObj.AddComponent<RectTransform>();
            rtT.anchorMin = new Vector2(0, 0);
            rtT.anchorMax = new Vector2(0, 0);
            rtT.pivot = new Vector2(0, 0);
            rtT.anchoredPosition = new Vector2(40f, 18f + i * 26f);
            rtT.sizeDelta = new Vector2(200f, 20f);
            Text txt = textoObj.AddComponent<Text>();
            txt.text = labels[i];
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 13;
            txt.color = Color.white;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    Color ColorSegunEleccion(TipoEleccion tipo)
    {
        switch (tipo)
        {
            case TipoEleccion.Verde: return colorVerde;
            case TipoEleccion.Rojo: return colorRojo;
            default: return colorGris;
        }
    }
}
