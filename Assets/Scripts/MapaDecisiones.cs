using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mapa de decisiones v3 — Layout COMPLETAMENTE HORIZONTAL.
///
/// PROBLEMA ANTERIOR: Las 3 tarjetas de cada día se apilaban verticalmente
/// hacia abajo, haciendo el mapa altísimo e imposible de ver sin scroll vertical.
///
/// SOLUCIÓN v3: Las 3 decisiones de cada día se muestran en FILA HORIZONTAL.
/// El hub del día queda arriba y sus 3 tarjetas se despliegan a continuación
/// hacia abajo, pero las 3 al mismo nivel Y.
///
///   [Día 1]──────────────[Día 2]──────────────[Día 3]──────────────[Día 4]──[DESENLACE]
///   [M1]  [M2]  [M3]     [M4]  [M5]  [M6]     [M7]  [M8]  [M9]   [M10][M11][M12]
///
/// SETUP EN UNITY:
///   - ScrollRect: horizontal ✓  |  vertical ✗
///   - Contenedor (Content): pivot (0, 0.5), anchor left-stretch
///   - Content Size Fitter en Contenedor: Horizontal = Preferred Size
///   - La ALTURA del Contenedor debe ser FIJA (igual a la del Viewport)
///   - LeyendaRaiz: RectTransform hermano del ScrollRect (FUERA de él)
/// </summary>
public class MapaDecisiones : MonoBehaviour
{
    [Header("── Contenedor (Content del ScrollRect) ──")]
    public RectTransform contenedor;

    [Header("── Leyenda fija (fuera del ScrollRect) ──")]
    public RectTransform leyendaRaiz;

    [Header("── Prefabs ─────────────────────────────")]
    public GameObject prefabNodo;
    public GameObject prefabLinea;

    [Header("── Colores ─────────────────────────────")]
    public Color colorVerde = new Color(0.11f, 0.73f, 0.33f);
    public Color colorRojo = new Color(0.85f, 0.15f, 0.15f);
    public Color colorGris = new Color(0.50f, 0.50f, 0.50f);
    public Color colorLinea = new Color(1f, 1f, 1f, 0.35f);
    public Color colorDia = new Color(0.14f, 0.42f, 0.48f);

    [Header("── Layout ───────────────────────────────")]
    [Tooltip("Ancho de cada bloque de día (cubre sus 3 momentos en fila)")]
    public float anchoBloquesDia = 420f;
    [Tooltip("Espacio entre bloques de día")]
    public float gapEntreBloques = 20f;
    [Tooltip("Margen izquierdo")]
    public float margenIzquierdo = 60f;
    [Tooltip("Margen derecho después de la tarjeta final")]
    public float margenDerecho = 60f;
    [Tooltip("Posición Y del hub de día (positivo=arriba del centro del contenedor)")]
    public float yHub = 85f;
    [Tooltip("Posición Y de las tarjetas de momentos (negativo=abajo del centro)")]
    public float yMomentos = -70f;
    [Tooltip("Grosor de líneas")]
    public float grosorLinea = 3f;

    [Header("── Tamaños ─────────────────────────────")]
    public float anchoHub = 110f;
    public float altoHub = 48f;
    public float anchoTarjeta = 118f;
    public float altoTarjeta = 120f;
    public float anchoTarjetaFinal = 200f;
    public float altoTarjetaFinal = 160f;

    [Header("── Texto ───────────────────────────────")]
    public int fsHub = 13;
    public int fsTitTarj = 11;
    public int fsCpoTarj = 10;
    public int fsTitFinal = 13;
    public int fsCpoFinal = 11;
    public float padTarjeta = 7f;
    [Range(0.2f, 0.5f)]
    public float fraccionTitulo = 0.32f;

    [Header("── Nombres ─────────────────────────────")]
    public string[] nombresMomentos = {
        "Primer Contacto", "Juego",       "Cierre",
        "Reencuentro",     "Emoción",     "Vínculo",
        "Rutina",          "Información", "Confianza",
        "Canal",           "Secreto",     "Encuentro"
    };
    public string[] nombresDias = { "Día 1", "Día 2", "Día 3", "Día 4" };

    void Start()
    {
        if (GameManager.Instance == null) { Debug.LogWarning("[MapaDecisiones] No GameManager."); return; }
        GenerarMapa();
        ConstruirLeyenda();
    }

    void GenerarMapa()
    {
        GameManager gm = GameManager.Instance;
        TipoEleccion[] his = gm.HistorialElecciones;
        string[] txt = gm.HistorialTextos;

        float sepInterna = anchoBloquesDia / 3f; // ancho asignado a cada momento

        for (int d = 0; d < GameManager.TOTAL_DIAS; d++)
        {
            float xBloqueCentro = margenIzquierdo
                + d * (anchoBloquesDia + gapEntreBloques)
                + anchoBloquesDia * 0.5f;

            // Hub del día
            string nomDia = (nombresDias != null && d < nombresDias.Length) ? nombresDias[d] : $"Día {d + 1}";
            CrearTarjeta(new Vector2(xBloqueCentro, yHub),
                anchoHub, altoHub, colorDia,
                nomDia, null, fsHub, fsCpoTarj);

            // Línea entre hubs
            if (d > 0)
            {
                float xPrev = margenIzquierdo + (d - 1) * (anchoBloquesDia + gapEntreBloques) + anchoBloquesDia * 0.5f;
                CrearLinea(new Vector2(xPrev + anchoHub * 0.5f, yHub),
                           new Vector2(xBloqueCentro - anchoHub * 0.5f, yHub));
            }

            // 3 tarjetas en fila horizontal
            float xBloqueIzq = xBloqueCentro - anchoBloquesDia * 0.5f;
            for (int m = 0; m < GameManager.DECISIONES_POR_DIA; m++)
            {
                int i = d * GameManager.DECISIONES_POR_DIA + m;
                if (i >= GameManager.TOTAL_MOMENTOS) break;

                float xMom = xBloqueIzq + sepInterna * (m + 0.5f);
                string nomMom = (nombresMomentos != null && i < nombresMomentos.Length) ? nombresMomentos[i] : $"M{i + 1}";
                string textoEleg = (txt != null && i < txt.Length) ? txt[i] : "";

                CrearTarjeta(new Vector2(xMom, yMomentos),
                    anchoTarjeta, altoTarjeta,
                    ColorSegunEleccion(his[i]),
                    nomMom, textoEleg,
                    fsTitTarj, fsCpoTarj);

                // Línea desde hub hacia abajo hasta la tarjeta
                CrearLinea(new Vector2(xMom, yHub - altoHub * 0.5f),
                           new Vector2(xMom, yMomentos + altoTarjeta * 0.5f));
            }
        }

        // Tarjeta final
        float xUltimoBloque = margenIzquierdo + (GameManager.TOTAL_DIAS - 1) * (anchoBloquesDia + gapEntreBloques) + anchoBloquesDia * 0.5f;
        float xFinal = xUltimoBloque + anchoHub * 0.5f + gapEntreBloques + anchoTarjetaFinal * 0.5f;
        CrearLinea(new Vector2(xUltimoBloque + anchoHub * 0.5f, yHub),
                   new Vector2(xFinal - anchoTarjetaFinal * 0.5f, yHub));
        CrearTarjetaFinal(new Vector2(xFinal, yHub));

        float anchoTotal = xFinal + anchoTarjetaFinal * 0.5f + margenDerecho;
        if (contenedor != null)
            contenedor.sizeDelta = new Vector2(anchoTotal, contenedor.sizeDelta.y);
    }

    void CrearTarjetaFinal(Vector2 pos)
    {
        GameManager gm = GameManager.Instance;
        Color color = gm.EsFinal1 ? colorVerde : colorRojo;
        string titulo = gm.ObtenerTituloFinal();
        string cuerpo = $"Confianza: {gm.PuntosConfianza} pts\n"
                        + $"Riesgo: {gm.PuntosRiesgo} pts\n\n"
                        + gm.ObtenerMensajeFinal();

        GameObject go = CrearTarjeta(pos, anchoTarjetaFinal, altoTarjetaFinal,
            color, titulo, cuerpo, fsTitFinal, fsCpoFinal);

        Outline ol = go.AddComponent<Outline>();
        ol.effectColor = new Color(1f, 1f, 1f, 0.85f);
        ol.effectDistance = new Vector2(2f, -2f);
    }

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

        Text tPrefab = go.GetComponentInChildren<Text>();
        if (tPrefab != null) tPrefab.gameObject.SetActive(false);

        bool tieneCuerpo = !string.IsNullOrEmpty(cuerpo);
        Vector2 aMinTit = tieneCuerpo ? new Vector2(0f, 1f - fraccionTitulo) : Vector2.zero;
        AgregarTexto(rt, "Titulo", aMinTit, Vector2.one,
            padTarjeta, titulo, fsTit, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter, false);

        if (tieneCuerpo)
        {
            float topCpo = 1f - fraccionTitulo - 0.04f;
            AgregarTexto(rt, "Cuerpo", Vector2.zero, new Vector2(1f, topCpo),
                padTarjeta, cuerpo, fsCpo,
                new Color(1f, 1f, 1f, 0.95f), FontStyle.Normal, TextAnchor.UpperCenter, true);
        }
        return go;
    }

    void AgregarTexto(RectTransform padre, string nombre,
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
        go.transform.SetAsFirstSibling();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  LEYENDA FIJA
    // ═══════════════════════════════════════════════════════════════════
    void ConstruirLeyenda()
    {
        if (leyendaRaiz == null)
        {
            Debug.LogWarning("[MapaDecisiones] Asigna 'leyendaRaiz' (RectTransform fuera del ScrollRect).");
            return;
        }

        foreach (Transform h in leyendaRaiz) Destroy(h.gameObject);

        string[] etiquetas = { "Decisión de confianza", "Decisión neutra", "Decisión riesgosa" };
        Color[] colores = { colorVerde, colorGris, colorRojo };

        float cuad = 16f;
        float filH = 24f;
        float padX = 10f;
        float padY = 8f;
        float titH = 20f;
        float totalH = padY + titH + 6f + etiquetas.Length * (filH + 4f) + padY;
        leyendaRaiz.sizeDelta = new Vector2(leyendaRaiz.sizeDelta.x, totalH);

        // Fondo
        GameObject bg = new GameObject("Fondo");
        bg.transform.SetParent(leyendaRaiz, false);
        RectTransform rtBg = bg.AddComponent<RectTransform>();
        rtBg.anchorMin = Vector2.zero; rtBg.anchorMax = Vector2.one;
        rtBg.offsetMin = Vector2.zero; rtBg.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

        // Título
        float yNow = -padY;
        TexEn(leyendaRaiz, "Tit", new Vector2(padX, yNow), new Vector2(200f, titH),
              "Tipos de decisión:", 14, FontStyle.Bold, Color.white);
        yNow -= (titH + 6f);

        for (int i = 0; i < etiquetas.Length; i++)
        {
            GameObject sq = new GameObject($"Sq{i}");
            sq.transform.SetParent(leyendaRaiz, false);
            RectTransform rtSq = sq.AddComponent<RectTransform>();
            rtSq.anchorMin = new Vector2(0, 1); rtSq.anchorMax = new Vector2(0, 1);
            rtSq.pivot = new Vector2(0, 1);
            rtSq.anchoredPosition = new Vector2(padX, yNow);
            rtSq.sizeDelta = new Vector2(cuad, cuad);
            sq.AddComponent<Image>().color = colores[i];

            TexEn(leyendaRaiz, $"Lbl{i}",
                  new Vector2(padX + cuad + 6f, yNow + 1f),
                  new Vector2(190f, cuad + 2f),
                  etiquetas[i], 13, FontStyle.Normal, Color.white);

            yNow -= (filH + 4f);
        }
    }

    void TexEn(RectTransform padre, string nom, Vector2 pos, Vector2 tam,
               string texto, int fs, FontStyle estilo, Color col)
    {
        GameObject obj = new GameObject(nom);
        obj.transform.SetParent(padre, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
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

