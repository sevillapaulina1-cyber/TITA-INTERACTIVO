using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MapaDecisiones v4
///
/// LAYOUT:
///   Scroll HORIZONTAL. Cada día = columna con hub arriba y 3 momentos en vertical debajo.
///   Las columnas están bien separadas para que se lea como "Día 1 | Día 2 | Día 3 | Día 4 | Desenlace".
///
///   [Día 1]        [Día 2]        [Día 3]        [Día 4]     [DESENLACE]
///    │               │               │               │
///   [M1]           [M4]           [M7]           [M10]
///   [M2]           [M5]           [M8]           [M11]
///   [M3]           [M6]           [M9]           [M12]
///
/// SETUP MÍNIMO EN UNITY (solo esto necesitas verificar):
///   ScrollRect:
///     horizontal = TRUE
///     vertical   = FALSE
///     movementType = Clamped
///   Contenedor (Content del ScrollRect):
///     ⚠ NO agregues Content Size Fitter — este script fija el sizeDelta manualmente.
///     Anchor: left-center (preset "left", con stretch vertical desactivado)
///     Pivot: (0, 0.5)
///   LeyendaRaiz:
///     RectTransform hermano del ScrollRect (fuera de él), esquina superior derecha.
///     Ancho: ~220. Alto: el script lo calcula automáticamente.
/// </summary>
public class MapaDecisiones : MonoBehaviour
{
    // ── Referencias ───────────────────────────────────────────────────────
    [Header("── Referencias ────────────────────────")]
    [Tooltip("Content del ScrollRect")]
    public RectTransform contenedor;
    [Tooltip("RectTransform FUERA del ScrollRect (hermano de él dentro de PanelRetro)")]
    public RectTransform leyendaRaiz;
    public GameObject prefabNodo;
    public GameObject prefabLinea;

    // ── Colores ───────────────────────────────────────────────────────────
    [Header("── Colores ─────────────────────────────")]
    public Color colorVerde = new Color(0.11f, 0.73f, 0.33f);
    public Color colorRojo = new Color(0.85f, 0.15f, 0.15f);
    public Color colorGris = new Color(0.50f, 0.50f, 0.50f);
    public Color colorLinea = new Color(1f, 1f, 1f, 0.30f);
    public Color colorDia = new Color(0.14f, 0.42f, 0.48f);

    // ── Layout ────────────────────────────────────────────────────────────
    [Header("── Layout ───────────────────────────────")]
    [Tooltip("Ancho de cada columna de día (hub + 3 momentos)")]
    public float anchoColumna = 170f;
    [Tooltip("Espacio entre columnas de días")]
    public float gapColumnas = 50f;
    [Tooltip("Margen izquierdo antes del primer día")]
    public float margenIzquierdo = 60f;
    [Tooltip("Margen derecho después de la tarjeta de desenlace")]
    public float margenDerecho = 60f;
    [Tooltip("Y del centro del hub de día desde el centro del contenedor (positivo = arriba)")]
    public float yHub = 120f;
    [Tooltip("Espacio vertical entre la base del hub y la cima del primer momento")]
    public float gapHubMomentos = 18f;
    [Tooltip("Espacio vertical entre tarjetas de momento consecutivas")]
    public float gapEntreMomentos = 12f;
    public float grosorLinea = 3f;

    // ── Tamaños de tarjetas ───────────────────────────────────────────────
    [Header("── Tamaños ─────────────────────────────")]
    public float anchoHub = 130f;
    public float altoHub = 46f;
    public float anchoMomento = 145f;
    public float altoMomento = 100f;
    public float anchoDesenlace = 190f;
    public float altoDesenlace = 170f;

    // ── Texto ─────────────────────────────────────────────────────────────
    [Header("── Texto ───────────────────────────────")]
    public int fsHub = 14;
    public int fsTitMom = 12;
    public int fsCpoMom = 10;
    public int fsTitDes = 13;
    public int fsCpoDes = 11;
    public float padTarjeta = 7f;
    [Range(0.2f, 0.45f)]
    public float fraccionTitulo = 0.30f;

    // ── Nombres ───────────────────────────────────────────────────────────
    [Header("── Nombres ─────────────────────────────")]
    [Tooltip("Nombres que aparecen en los hubs de día. Se respetan los valores del Inspector.")]
    public string[] nombresDias = new string[0];
    [Tooltip("Nombres de cada momento (12 en total). Se respetan los valores del Inspector.")]
    public string[] nombresMomentos = new string[0];

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[MapaDecisiones] GameManager no encontrado.");
            return;
        }

        // Forzar pivot del Contenedor por código para evitar problemas de setup
        if (contenedor != null)
        {
            contenedor.pivot = new Vector2(0f, 0.5f);
            contenedor.anchorMin = new Vector2(0f, 0f);
            contenedor.anchorMax = new Vector2(0f, 1f);
        }

        GenerarMapa();
        ConstruirLeyenda();
    }

    // ─────────────────────────────────────────────────────────────────────
    void GenerarMapa()
    {
        GameManager gm = GameManager.Instance;
        TipoEleccion[] his = gm.HistorialElecciones;
        string[] txt = gm.HistorialTextos;

        // Calcular la altura total de la columna de momentos para centrar verticalmente
        float altoColumna3 = GameManager.DECISIONES_POR_DIA * altoMomento
                           + (GameManager.DECISIONES_POR_DIA - 1) * gapEntreMomentos;
        // Y del centro del primer momento en la columna
        float yPrimerMom = yHub - altoHub * 0.5f - gapHubMomentos - altoMomento * 0.5f;

        for (int d = 0; d < GameManager.TOTAL_DIAS; d++)
        {
            // Centro X de la columna de este día
            float xCol = margenIzquierdo + d * (anchoColumna + gapColumnas) + anchoColumna * 0.5f;

            // ── Hub del día ──────────────────────────────────────────────
            string nomDia = (nombresDias != null && d < nombresDias.Length && !string.IsNullOrEmpty(nombresDias[d]))
                ? nombresDias[d] : $"Día {d + 1}";

            CrearTarjeta(new Vector2(xCol, yHub),
                anchoHub, altoHub, colorDia,
                nomDia, null, fsHub, fsCpoMom);

            // ── Línea horizontal entre hubs ─────────────────────────────
            if (d > 0)
            {
                float xColPrev = margenIzquierdo + (d - 1) * (anchoColumna + gapColumnas) + anchoColumna * 0.5f;
                CrearLinea(
                    new Vector2(xColPrev + anchoHub * 0.5f, yHub),
                    new Vector2(xCol - anchoHub * 0.5f, yHub));
            }

            // ── 3 momentos en columna vertical ──────────────────────────
            for (int m = 0; m < GameManager.DECISIONES_POR_DIA; m++)
            {
                int i = d * GameManager.DECISIONES_POR_DIA + m;
                if (i >= GameManager.TOTAL_MOMENTOS) break;

                float yMom = yPrimerMom - m * (altoMomento + gapEntreMomentos);

                string nomMom = (nombresMomentos != null && i < nombresMomentos.Length
                                 && !string.IsNullOrEmpty(nombresMomentos[i]))
                    ? nombresMomentos[i] : $"M{i + 1}";
                string eleccion = (txt != null && i < txt.Length) ? txt[i] : "";

                CrearTarjeta(new Vector2(xCol, yMom),
                    anchoMomento, altoMomento,
                    ColorSegunEleccion(his[i]),
                    nomMom, eleccion,
                    fsTitMom, fsCpoMom);

                // Línea vertical
                if (m == 0)
                    // Primera: desde base del hub hasta cima del momento
                    CrearLinea(
                        new Vector2(xCol, yHub - altoHub * 0.5f),
                        new Vector2(xCol, yMom + altoMomento * 0.5f));
                else
                    // Siguientes: desde base del momento anterior
                    CrearLinea(
                        new Vector2(xCol, yMom + altoMomento * 0.5f + gapEntreMomentos),
                        new Vector2(xCol, yMom + altoMomento * 0.5f));
            }
        }

        // ── Tarjeta de desenlace ─────────────────────────────────────────
        float xUltimaCol = margenIzquierdo
            + (GameManager.TOTAL_DIAS - 1) * (anchoColumna + gapColumnas)
            + anchoColumna * 0.5f;
        float xDesenlace = xUltimaCol
            + anchoColumna * 0.5f
            + gapColumnas
            + anchoDesenlace * 0.5f;

        CrearLinea(
            new Vector2(xUltimaCol + anchoHub * 0.5f, yHub),
            new Vector2(xDesenlace - anchoDesenlace * 0.5f, yHub));

        CrearTarjetaDesenlace(new Vector2(xDesenlace, yHub));

        // Ajustar ancho del Contenedor
        float anchoTotal = xDesenlace + anchoDesenlace * 0.5f + margenDerecho;
        if (contenedor != null)
            contenedor.sizeDelta = new Vector2(anchoTotal, contenedor.sizeDelta.y);
    }

    // ─────────────────────────────────────────────────────────────────────
    void CrearTarjetaDesenlace(Vector2 pos)
    {
        GameManager gm = GameManager.Instance;
        Color color = gm.EsFinal1 ? colorVerde : colorRojo;
        string titulo = gm.ObtenerTituloFinal();
        string cuerpo = $"Confianza: {gm.PuntosConfianza} pts\n"
                           + $"Riesgo:    {gm.PuntosRiesgo} pts\n\n"
                           + gm.ObtenerMensajeFinal();

        GameObject go = CrearTarjeta(pos,
            anchoDesenlace, altoDesenlace,
            color, titulo, cuerpo,
            fsTitDes, fsCpoDes);

        // Borde blanco para distinguirla visualmente
        Outline ol = go.AddComponent<Outline>();
        ol.effectColor = new Color(1f, 1f, 1f, 0.9f);
        ol.effectDistance = new Vector2(2.5f, -2.5f);
    }

    // ─────────────────────────────────────────────────────────────────────
    GameObject CrearTarjeta(Vector2 pos, float ancho, float alto,
                             Color colorFondo, string titulo, string cuerpo,
                             int fsTit, int fsCpo)
    {
        GameObject go = Instantiate(prefabNodo, contenedor);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(ancho, alto);

        Image img = go.GetComponent<Image>();
        if (img != null) img.color = colorFondo;

        // Desactivar Text del prefab base
        Text tBase = go.GetComponentInChildren<Text>();
        if (tBase != null) tBase.gameObject.SetActive(false);

        bool conCuerpo = !string.IsNullOrEmpty(cuerpo);

        // Zona del título (parte superior)
        Vector2 aMinTit = conCuerpo ? new Vector2(0f, 1f - fraccionTitulo) : Vector2.zero;
        Texto(rt, "Titulo", aMinTit, Vector2.one,
              padTarjeta, titulo, fsTit,
              Color.white, FontStyle.Bold, TextAnchor.MiddleCenter, false);

        // Zona del cuerpo (parte inferior)
        if (conCuerpo)
        {
            float topCpo = 1f - fraccionTitulo - 0.03f;
            Texto(rt, "Cuerpo", Vector2.zero, new Vector2(1f, topCpo),
                  padTarjeta, cuerpo, fsCpo,
                  new Color(1f, 1f, 1f, 0.95f), FontStyle.Normal, TextAnchor.UpperCenter, true);
        }

        return go;
    }

    // ─────────────────────────────────────────────────────────────────────
    void Texto(RectTransform padre, string nombre,
               Vector2 aMin, Vector2 aMax,
               float pad, string texto, int fs, Color col,
               FontStyle estilo, TextAnchor align, bool wrap)
    {
        GameObject obj = new GameObject(nombre);
        obj.transform.SetParent(padre, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = new Vector2(pad, pad);
        rt.offsetMax = new Vector2(-pad, -pad);
        Text t = obj.AddComponent<Text>();
        t.text = texto;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fs;
        t.fontStyle = estilo;
        t.color = col;
        t.alignment = align;
        t.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
    }

    // ─────────────────────────────────────────────────────────────────────
    void CrearLinea(Vector2 desde, Vector2 hasta)
    {
        if (prefabLinea == null) return;
        GameObject go = Instantiate(prefabLinea, contenedor);
        RectTransform rt = go.GetComponent<RectTransform>();
        Vector2 dir = hasta - desde;
        float dist = dir.magnitude;
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rt.anchoredPosition = desde + dir * 0.5f;
        rt.sizeDelta = new Vector2(dist, grosorLinea);
        rt.localRotation = Quaternion.Euler(0f, 0f, ang);
        Image img = go.GetComponent<Image>();
        if (img != null) img.color = colorLinea;
        go.transform.SetAsFirstSibling(); // detrás de las tarjetas
    }

    // ═══════════════════════════════════════════════════════════════════
    //  LEYENDA FIJA — se dibuja en leyendaRaiz, fuera del ScrollRect
    // ═══════════════════════════════════════════════════════════════════
    void ConstruirLeyenda()
    {
        if (leyendaRaiz == null)
        {
            Debug.LogWarning("[MapaDecisiones] Asigna 'leyendaRaiz' — un RectTransform " +
                             "hermano del ScrollRect (fuera de él).");
            return;
        }

        // Limpiar hijos previos
        for (int i = leyendaRaiz.childCount - 1; i >= 0; i--)
            Destroy(leyendaRaiz.GetChild(i).gameObject);

        string[] labels = { "Decisión de confianza", "Decisión neutra", "Decisión riesgosa" };
        Color[] cols = { colorVerde, colorGris, colorRojo };

        float cuad = 16f;
        float rowH = 26f;
        float pX = 12f;
        float pY = 10f;
        float titH = 22f;
        float totalH = pY + titH + 8f + labels.Length * (rowH + 5f) + pY;
        leyendaRaiz.sizeDelta = new Vector2(leyendaRaiz.sizeDelta.x, totalH);

        // Fondo semitransparente
        GameObject bg = new GameObject("BgLeyenda");
        bg.transform.SetParent(leyendaRaiz, false);
        RectTransform rtBg = bg.AddComponent<RectTransform>();
        rtBg.anchorMin = Vector2.zero; rtBg.anchorMax = Vector2.one;
        rtBg.offsetMin = Vector2.zero; rtBg.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 0.75f);

        // Título
        float y = -pY;
        LeyTexto(leyendaRaiz, "LTit",
                 new Vector2(pX, y), new Vector2(leyendaRaiz.sizeDelta.x - pX * 2f, titH),
                 "Tipos de decisión:", 15, FontStyle.Bold, Color.white);
        y -= (titH + 8f);

        // Filas: cuadrado de color + etiqueta
        for (int i = 0; i < labels.Length; i++)
        {
            // Cuadrado de color
            GameObject sq = new GameObject($"Sq{i}");
            sq.transform.SetParent(leyendaRaiz, false);
            RectTransform rtSq = sq.AddComponent<RectTransform>();
            rtSq.anchorMin = new Vector2(0, 1);
            rtSq.anchorMax = new Vector2(0, 1);
            rtSq.pivot = new Vector2(0, 1);
            rtSq.anchoredPosition = new Vector2(pX, y);
            rtSq.sizeDelta = new Vector2(cuad, cuad);
            sq.AddComponent<Image>().color = cols[i];

            // Etiqueta
            LeyTexto(leyendaRaiz, $"Lbl{i}",
                     new Vector2(pX + cuad + 7f, y + 1f),
                     new Vector2(leyendaRaiz.sizeDelta.x - pX - cuad - 7f, cuad + 2f),
                     labels[i], 13, FontStyle.Normal, Color.white);

            y -= (rowH + 5f);
        }
    }

    void LeyTexto(RectTransform padre, string nom,
                  Vector2 pos, Vector2 tam,
                  string texto, int fs, FontStyle estilo, Color col)
    {
        GameObject obj = new GameObject(nom);
        obj.transform.SetParent(padre, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        Text t = obj.AddComponent<Text>();
        t.text = texto;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fs;
        t.fontStyle = estilo;
        t.color = col;
        t.alignment = TextAnchor.MiddleLeft;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
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

