using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mapa CONCEPTUAL HORIZONTAL de decisiones para la pantalla de retroalimentación.
///
/// CAMBIOS v2:
///   • La LEYENDA ya NO va dentro del ScrollRect/Contenedor.
///     Se crea en un RectTransform fijo (leyendaRaiz) que debe ser hermano del
///     ScrollRect dentro de PanelRetro, así no se mueve al hacer scroll.
///   • Tamaños de tarjeta y separaciones reducidos para que quepan en pantalla.
///   • Fuente más grande y legible para padres de familia.
///   • Tarjeta final con mensaje del desenlace bien visible.
///
/// SETUP EN UNITY:
///   Canvas
///   └── PanelRetro
///       ├── ZonaSuperior        (Puntos, título final, botón Reiniciar — NO es parte de este script)
///       ├── LeyendaRaiz         ← RectTransform FIJO (esquina superior-derecha o inferior-izquierda)
///       │                          Asígnalo al campo "leyendaRaiz" de este script.
///       └── ScrollRect
///           ├── Viewport
///           └── Contenedor      ← Content del ScrollRect; asignarlo al campo "contenedor"
///               └── MapaDecisionesGO  ← este script
///
///   ScrollRect: horizontal ✓ | vertical ✗
///   Content Size Fitter en Contenedor: Horizontal = Preferred Size, Vertical = None
///   Anchor/pivot del Contenedor: izquierda-centro, pivot (0, 0.5)
/// </summary>
public class MapaDecisiones : MonoBehaviour
{
    // ── Contenedor (Content del ScrollRect) ──────────────────────────────
    [Header("── Contenedor (Content del ScrollRect) ──")]
    [Tooltip("El RectTransform marcado como Content en el ScrollRect horizontal")]
    public RectTransform contenedor;

    // ── Leyenda FIJA (fuera del ScrollRect) ──────────────────────────────
    [Header("── Leyenda fija (FUERA del ScrollRect) ──")]
    [Tooltip("RectTransform hermano del ScrollRect, dentro de PanelRetro. " +
             "La leyenda se construye aquí y NO se mueve al hacer scroll.")]
    public RectTransform leyendaRaiz;

    // ── Prefabs ───────────────────────────────────────────────────────────
    [Header("── Prefabs ─────────────────────────────")]
    public GameObject prefabNodo;   // Image (rectángulo redondeado) con Text hijo
    public GameObject prefabLinea;  // Image como segmento de línea

    // ── Colores ───────────────────────────────────────────────────────────
    [Header("── Colores ─────────────────────────────")]
    public Color colorVerde = new Color(0.11f, 0.73f, 0.33f);
    public Color colorRojo = new Color(0.85f, 0.15f, 0.15f);
    public Color colorGris = new Color(0.55f, 0.55f, 0.55f);
    public Color colorLinea = new Color(1f, 1f, 1f, 0.40f);
    public Color colorDia = new Color(0.14f, 0.42f, 0.48f);
    public Color colorFinal1 = new Color(0.11f, 0.73f, 0.33f); // Verde
    public Color colorFinal2 = new Color(0.85f, 0.15f, 0.15f); // Rojo

    // ── Layout horizontal ─────────────────────────────────────────────────
    [Header("── Layout horizontal ───────────────────")]
    [Tooltip("Distancia horizontal entre centros de día consecutivos")]
    public float separacionEntreDias = 340f;
    [Tooltip("Distancia vertical entre el hub de día y cada tarjeta de momento")]
    public float separacionVerticalMomentos = 150f;
    [Tooltip("Margen izquierdo antes del primer hub de día")]
    public float margenIzquierdo = 120f;
    [Tooltip("Grosor de las líneas conectoras")]
    public float grosorLinea = 3f;

    // ── Tamaños de tarjetas ───────────────────────────────────────────────
    [Header("── Tamaños de tarjetas ─────────────────")]
    public float anchoHubDia = 110f;
    public float altoHubDia = 55f;
    public float anchoTarjeta = 185f;
    public float altoTarjeta = 145f;
    public float anchoTarjetaFinal = 240f;
    public float altoTarjetaFinal = 200f;

    // ── Texto dentro de las tarjetas ──────────────────────────────────────
    [Header("── Texto dentro de las tarjetas ─────────")]
    [Tooltip("Muestra la opción exacta que eligió el jugador")]
    public bool mostrarTextoEleccion = true;
    [Tooltip("Padding interno de las tarjetas")]
    public float paddingInternoTarjeta = 10f;
    [Tooltip("Fracción de altura reservada para el título")]
    [Range(0.2f, 0.5f)]
    public float alturaRelativaTitulo = 0.30f;
    [Tooltip("Separación extra entre título y cuerpo")]
    [Range(0f, 0.1f)]
    public float separacionTituloCuerpo = 0.04f;
    public int tamanoFuenteHub = 15;
    public int tamanoFuenteTitulo = 13;
    public int tamanoFuenteEleccion = 11;
    public int tamanoFuenteFinalTit = 14;
    public int tamanoFuenteFinalCpo = 12;
    public Color colorTitulo = Color.white;
    public Color colorTextoEleccion = new Color(1f, 1f, 1f, 0.95f);

    // ── Nombres ───────────────────────────────────────────────────────────
    [Header("── Nombres de momentos ──────────────────")]
    public string[] nombresMomentos = {
        "Primer Contacto", "Juego",       "Volvemos a vernos",
        "Reencuentro",     "Emoción",     "Vínculo",
        "Rutina",          "Información", "Confianza",
        "Canal",           "Secreto",     "Encuentro"
    };
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
        ConstruirLeyenda();   // ← leyenda fija, FUERA del ScrollRect
    }

    // ─────────────────────────────────────────────────────────────────────
    void GenerarMapa()
    {
        GameManager gm = GameManager.Instance;
        TipoEleccion[] historial = gm.HistorialElecciones;
        string[] textos = gm.HistorialTextos;

        float yHub = 0f; // hub de cada día centrado en Y=0

        for (int d = 0; d < GameManager.TOTAL_DIAS; d++)
        {
            float xDia = margenIzquierdo + d * separacionEntreDias;

            // ── Hub del día ────────────────────────────────────────────────
            string nombreDia = (nombresDias != null && d < nombresDias.Length && !string.IsNullOrEmpty(nombresDias[d]))
                ? nombresDias[d] : $"Día {d + 1}";

            CrearTarjeta(new Vector2(xDia, yHub),
                anchoHubDia, altoHubDia, colorDia,
                nombreDia, null,
                tamanoFuenteHub, tamanoFuenteEleccion);

            // ── Línea entre hubs ───────────────────────────────────────────
            if (d > 0)
            {
                float xPrev = margenIzquierdo + (d - 1) * separacionEntreDias;
                CrearLinea(new Vector2(xPrev + anchoHubDia * 0.5f, yHub),
                           new Vector2(xDia - anchoHubDia * 0.5f, yHub));
            }

            // ── 3 tarjetas de momentos ramificadas hacia abajo ─────────────
            for (int m = 0; m < GameManager.DECISIONES_POR_DIA; m++)
            {
                int i = d * GameManager.DECISIONES_POR_DIA + m;
                if (i >= GameManager.TOTAL_MOMENTOS) break;

                float yMomento = yHub - altoHubDia * 0.5f - (m + 0.5f) * separacionVerticalMomentos;
                Vector2 posMom = new Vector2(xDia, yMomento);

                string nombreMom = (nombresMomentos != null && i < nombresMomentos.Length && !string.IsNullOrEmpty(nombresMomentos[i]))
                    ? nombresMomentos[i] : $"Momento {i + 1}";

                string textoEleg = (textos != null && i < textos.Length) ? textos[i] : "";

                CrearTarjeta(posMom, anchoTarjeta, altoTarjeta,
                    ColorSegunEleccion(historial[i]),
                    nombreMom,
                    mostrarTextoEleccion ? textoEleg : null,
                    tamanoFuenteTitulo, tamanoFuenteEleccion);

                // Línea vertical desde hub hasta tarjeta
                CrearLinea(new Vector2(xDia, yHub - altoHubDia * 0.5f),
                           new Vector2(posMom.x, posMom.y + altoTarjeta * 0.5f));
            }
        }

        // ── Tarjeta final a la derecha ─────────────────────────────────────
        float xFinal = margenIzquierdo + GameManager.TOTAL_DIAS * separacionEntreDias;
        float xUltimoDia = margenIzquierdo + (GameManager.TOTAL_DIAS - 1) * separacionEntreDias;

        CrearLinea(new Vector2(xUltimoDia + anchoHubDia * 0.5f, yHub),
                   new Vector2(xFinal - anchoTarjetaFinal * 0.5f, yHub));

        CrearTarjetaFinal(new Vector2(xFinal, yHub));

        // Ajustar ancho del Contenedor para que el ScrollRect funcione
        float anchoTotal = xFinal + anchoTarjetaFinal * 0.5f + margenIzquierdo;
        if (contenedor != null)
            contenedor.sizeDelta = new Vector2(anchoTotal, contenedor.sizeDelta.y);
    }

    // ─────────────────────────────────────────────────────────────────────
    void CrearTarjetaFinal(Vector2 pos)
    {
        GameManager gm = GameManager.Instance;
        string titulo = gm.ObtenerTituloFinal();
        string puntos = $"Confianza: {gm.PuntosConfianza} pts   Riesgo: {gm.PuntosRiesgo} pts";
        string mensaje = gm.ObtenerMensajeFinal();
        string cuerpo = $"{puntos}\n\n{mensaje}";
        Color colorFondo = gm.EsFinal1 ? colorFinal1 : colorFinal2;

        GameObject tarjeta = CrearTarjeta(pos,
            anchoTarjetaFinal, altoTarjetaFinal,
            colorFondo, titulo, cuerpo,
            tamanoFuenteFinalTit, tamanoFuenteFinalCpo);

        // Borde blanco para que destaque
        Outline outline = tarjeta.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.8f);
        outline.effectDistance = new Vector2(2f, -2f);
    }

    // ─────────────────────────────────────────────────────────────────────
    GameObject CrearTarjeta(Vector2 pos, float ancho, float alto,
                             Color colorFondo, string titulo, string cuerpo,
                             int fsTitulo, int fsCuerpo)
    {
        GameObject tarjeta = Instantiate(prefabNodo, contenedor);
        RectTransform rt = tarjeta.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(ancho, alto);

        Image img = tarjeta.GetComponent<Image>();
        if (img != null) img.color = colorFondo;

        // Desactivar el Text del prefab original
        Text textoPrefab = tarjeta.GetComponentInChildren<Text>();
        if (textoPrefab != null) textoPrefab.gameObject.SetActive(false);

        bool tieneCuerpo = !string.IsNullOrEmpty(cuerpo);

        Vector2 anchorMinTitulo = tieneCuerpo
            ? new Vector2(0f, 1f - alturaRelativaTitulo)
            : Vector2.zero;

        CrearTextoHijo(rt, "Titulo",
            anchorMinTitulo, Vector2.one,
            paddingInternoTarjeta, titulo,
            fsTitulo, colorTitulo,
            FontStyle.Bold, TextAnchor.MiddleCenter, false);

        if (tieneCuerpo)
        {
            float topCuerpo = 1f - alturaRelativaTitulo - separacionTituloCuerpo;
            CrearTextoHijo(rt, "Cuerpo",
                Vector2.zero, new Vector2(1f, topCuerpo),
                paddingInternoTarjeta, cuerpo,
                fsCuerpo, colorTextoEleccion,
                FontStyle.Normal, TextAnchor.UpperCenter, true);
        }

        return tarjeta;
    }

    // ─────────────────────────────────────────────────────────────────────
    void CrearTextoHijo(RectTransform padre, string nombre,
                         Vector2 anchorMin, Vector2 anchorMax,
                         float padding, string texto,
                         int fontSize, Color color,
                         FontStyle estilo, TextAnchor alineacion, bool wrap)
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
        txt.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
    }

    // ─────────────────────────────────────────────────────────────────────
    void CrearLinea(Vector2 desde, Vector2 hasta)
    {
        if (prefabLinea == null) return;

        GameObject linea = Instantiate(prefabLinea, contenedor);
        RectTransform rt = linea.GetComponent<RectTransform>();

        Vector2 dir = hasta - desde;
        float distancia = dir.magnitude;
        float angulo = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        rt.anchoredPosition = desde + dir * 0.5f;
        rt.sizeDelta = new Vector2(distancia, grosorLinea);
        rt.localRotation = Quaternion.Euler(0, 0, angulo);

        Image img = linea.GetComponent<Image>();
        if (img != null) img.color = colorLinea;

        linea.transform.SetAsFirstSibling(); // detrás de las tarjetas
    }

    // ═════════════════════════════════════════════════════════════════════
    //  LEYENDA FIJA — se construye en leyendaRaiz, FUERA del ScrollRect
    // ═════════════════════════════════════════════════════════════════════
    void ConstruirLeyenda()
    {
        if (leyendaRaiz == null)
        {
            Debug.LogWarning("[MapaDecisiones] 'leyendaRaiz' no asignado. " +
                             "La leyenda no se mostrará. " +
                             "Crea un RectTransform hermano del ScrollRect y asígnalo.");
            return;
        }

        // Datos de la leyenda
        string[] etiquetas = {
            "Decisión de confianza (verde)",
            "Decisión neutra (gris)",
            "Decisión riesgosa (roja)"
        };
        Color[] colores = { colorVerde, colorGris, colorRojo };

        float alturaFila = 28f;
        float cuadrado = 18f;
        float margenX = 12f;
        float margenY = 12f;

        // Fondo semitransparente para la leyenda
        GameObject fondo = new GameObject("LeyendaFondo");
        fondo.transform.SetParent(leyendaRaiz, false);
        RectTransform rtFondo = fondo.AddComponent<RectTransform>();
        rtFondo.anchorMin = Vector2.zero;
        rtFondo.anchorMax = Vector2.one;
        rtFondo.offsetMin = Vector2.zero;
        rtFondo.offsetMax = Vector2.zero;
        Image imgFondo = fondo.AddComponent<Image>();
        imgFondo.color = new Color(0f, 0f, 0f, 0.55f);

        // Título de la leyenda
        GameObject tituloObj = new GameObject("LeyendaTitulo");
        tituloObj.transform.SetParent(leyendaRaiz, false);
        RectTransform rtTit = tituloObj.AddComponent<RectTransform>();
        rtTit.anchorMin = new Vector2(0f, 1f);
        rtTit.anchorMax = new Vector2(1f, 1f);
        rtTit.pivot = new Vector2(0f, 1f);
        rtTit.anchoredPosition = new Vector2(margenX, -margenY);
        rtTit.sizeDelta = new Vector2(-margenX * 2f, alturaFila);
        Text txtTit = tituloObj.AddComponent<Text>();
        txtTit.text = "Tipos de decisión:";
        txtTit.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txtTit.fontSize = 15;
        txtTit.fontStyle = FontStyle.Bold;
        txtTit.color = Color.white;
        txtTit.alignment = TextAnchor.MiddleLeft;

        // Filas de la leyenda
        for (int i = 0; i < etiquetas.Length; i++)
        {
            float yOffset = -margenY - alturaFila - (i * (alturaFila + 4f));

            // Cuadrado de color
            GameObject sq = new GameObject($"LeyendaSq{i}");
            sq.transform.SetParent(leyendaRaiz, false);
            RectTransform rtSq = sq.AddComponent<RectTransform>();
            rtSq.anchorMin = new Vector2(0f, 1f);
            rtSq.anchorMax = new Vector2(0f, 1f);
            rtSq.pivot = new Vector2(0f, 1f);
            rtSq.anchoredPosition = new Vector2(margenX, yOffset);
            rtSq.sizeDelta = new Vector2(cuadrado, cuadrado);
            sq.AddComponent<Image>().color = colores[i];

            // Etiqueta de texto
            GameObject lbl = new GameObject($"LeyendaLbl{i}");
            lbl.transform.SetParent(leyendaRaiz, false);
            RectTransform rtLbl = lbl.AddComponent<RectTransform>();
            rtLbl.anchorMin = new Vector2(0f, 1f);
            rtLbl.anchorMax = new Vector2(1f, 1f);
            rtLbl.pivot = new Vector2(0f, 1f);
            rtLbl.anchoredPosition = new Vector2(margenX + cuadrado + 8f, yOffset + 2f);
            rtLbl.sizeDelta = new Vector2(-(margenX * 2f + cuadrado + 8f), cuadrado + 2f);
            Text txtLbl = lbl.AddComponent<Text>();
            txtLbl.text = etiquetas[i];
            txtLbl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtLbl.fontSize = 14;
            txtLbl.color = Color.white;
            txtLbl.alignment = TextAnchor.MiddleLeft;
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

