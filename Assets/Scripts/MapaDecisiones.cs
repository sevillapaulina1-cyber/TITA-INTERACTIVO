using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MapaDecisiones v10 — MISMA LÓGICA DE POSICIONAMIENTO que la versión simple
/// original (la de la "onda" que sí funcionaba): todo se construye alrededor
/// de (0,0) y el Contenedor queda anclado y centrado en su panel por código,
/// SIN medir ningún Viewport en tiempo real, SIN ScrollRect, SIN coroutines
/// esperando frames. Eso era la fuente de todos los bugs de centrado.
///
/// Se conserva el diseño actual (columnas por día, tarjetas grandes con
/// título + cuerpo, leyenda, tarjeta de desenlace, colores según elección).
///
/// LAYOUT: columnas verticales por día (ancho variable, se ajusta al texto del título).
///   [Día X]   [Día X]   [Día X]   [Día X]   [DESENLACE]
///    [M1]      [M4]      [M7]      [M10]
///    [M2]      [M5]      [M8]      [M11]
///    [M3]      [M6]      [M9]      [M12]
///
/// CÓMO SE CENTRA (igual que el script viejo):
///   1. Primero se calculan TODAS las posiciones X de las columnas (sin crear
///      nada todavía), lo que da el ancho total real del mapa (anchoTotal).
///   2. Se le resta anchoTotal/2 a cada posición, para que el mapa quede
///      construido simétrico alrededor de x=0 (idéntico a como el script
///      viejo hacía "startX = -(separacionHorizontal * 5.5f)").
///   3. El Contenedor se fuerza por código a pivot=(0.5,0.5), anchor
///      centrado en su panel, anchoredPosition=(0,0) — así su centro SIEMPRE
///      coincide con el centro del panel, sin importar tamaño de pantalla.
///   4. Cada tarjeta y línea se crea con pivot/anchor (0.5,0.5) también
///      forzado por código (no depende de cómo esté armado el prefab).
///
/// ESCALA: como ya no se mide ningún Viewport en tiempo real (eso causaba
///   los bugs), el tamaño del mapa se controla con 'escalaGlobal', un
///   número fijo que ajustas a mano en el Inspector para tu resolución
///   objetivo (por ejemplo 0.6 si el mapa se ve muy grande). Es exactamente
///   el mismo enfoque que usaba el script viejo con sus constantes fijas
///   (separacionHorizontal, tamanoNodo, etc.).
///
/// SETUP EN UNITY:
///   PanelMapa (opcional): el panel visual donde vive el mapa. Si lo asignas,
///              se le agrega un Mask/RectMask2D de seguridad para que nada se
///              salga de sus límites, pase lo que pase con 'escalaGlobal'.
///   Contenedor: hijo directo de PanelMapa (o de donde sea). El script lo
///              centra por código, no hace falta configurarlo a mano.
///   LeyendaRaiz: RectTransform aparte (no depende del mapa ni del panel).
/// </summary>
public class MapaDecisiones : MonoBehaviour
{
    // ── Referencias ───────────────────────────────────────────────────────
    [Header("── Referencias ────────────────────────")]
    public RectTransform contenedor;
    [Tooltip("Opcional: el panel visual donde vive el mapa. Si lo asignas, se " +
             "le agrega un Mask/RectMask2D de seguridad (nada se sale de ahí).")]
    public RectTransform panelMapa;
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

    // ── Escala manual (reemplaza el auto-ajuste al Viewport) ───────────────
    [Header("── Escala manual ────────────────────────")]
    [Tooltip("Multiplica TODOS los tamaños base de una sola vez. Ajústalo a " +
             "mano según tu resolución (igual que hacía el script viejo con " +
             "constantes fijas). 1 = tamaño base tal cual está abajo.")]
    public float escalaGlobal = 1f;

    // ── Layout BASE (se multiplican por escalaGlobal) ──────────────────────
    [Header("── Layout base (× escalaGlobal) ─────────")]
    [Tooltip("Ancho de cada columna de día")]
    public float anchoColumna = 100f;
    [Tooltip("Espacio horizontal entre columnas")]
    public float gapColumnas = 55f;
    [Tooltip("Margen izquierdo y derecho del mapa completo")]
    public float margenIzquierdo = 50f;
    public float margenDerecho = 50f;
    [Tooltip("Espacio entre la base del hub y la cima del primer momento")]
    public float gapHubMomentos = 20f;
    [Tooltip("Espacio entre tarjetas de momento consecutivas")]
    public float gapEntreMomentos = 14f;
    public float grosorLinea = 3f;

    // ── Tamaños BASE de tarjetas (× escalaGlobal) ───────────────────────────
    [Header("── Tamaños base (× escalaGlobal) ─────────")]
    public float anchoHub = 160f;
    public float altoHub = 52f;
    public float anchoMomento = 190f;
    public float altoMomento = 130f;
    public float anchoDesenlace = 310f;
    public float altoDesenlace = 280f;

    // ── Texto BASE (× escalaGlobal) ─────────────────────────────────────────
    [Header("── Texto base (× escalaGlobal) ──────────")]
    public int fsHub = 16;
    public int fsTitMom = 14;
    public int fsCpoMom = 12;
    public int fsTitDes = 18;
    public int fsCpoDes = 14;
    public float padTarjeta = 9f;
    [Range(0.2f, 0.45f)]
    public float fraccionTitulo = 0.28f;

    // ── Nombres (respeta lo del Inspector) ───────────────────────────────
    [Header("── Nombres (Inspector) ───────────────────")]
    public string[] nombresDias = new string[0];
    public string[] nombresMomentos = new string[0];

    // ── Debug (borrar/desactivar cuando ya no haga falta) ──────────────────
    [Header("── Debug ────────────────────────────────")]
    public bool mostrarDebugEnPantalla = false;

    // ── Posición del mapa dentro de su panel ────────────────────────────────
    [Header("── Posición ─────────────────────────────")]
    [Tooltip("Mueve el mapa completo hacia abajo (negativo) o hacia arriba " +
             "(positivo) sin descentrarlo horizontalmente. Ajusta a mano.")]
    public float desplazamientoY = 0f;
    [Tooltip("Igual, pero horizontal: mueve el mapa a la izquierda (negativo) " +
             "o derecha (positivo).")]
    public float desplazamientoX = 0f;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[MapaDecisiones] GameManager no encontrado.");
            return;
        }

        // ── Contenedor: centrado en su panel, igual que el script viejo ──
        // Nada de medir Viewport ni esperar frames — esto es instantáneo y
        // matemáticamente exacto, sin importar la resolución de pantalla.
        // 'desplazamientoX/Y' permiten correr el mapa sin descentrarlo.
        if (contenedor != null)
        {
            contenedor.pivot = new Vector2(0.5f, 0.5f);
            contenedor.anchorMin = new Vector2(0.5f, 0.5f);
            contenedor.anchorMax = new Vector2(0.5f, 0.5f);
            contenedor.anchoredPosition = new Vector2(desplazamientoX, desplazamientoY);
        }

        // Seguro opcional: si asignaste 'panelMapa', que nada se salga de ahí.
        if (panelMapa != null && panelMapa.GetComponent<RectMask2D>() == null)
            panelMapa.gameObject.AddComponent<RectMask2D>();

        // Por si queda un ScrollRect de una versión anterior en la jerarquía:
        // apagarlo del todo. Ya no se necesita para nada.
        ScrollRect srLegado = contenedor != null ? contenedor.GetComponentInParent<ScrollRect>() : null;
        if (srLegado != null)
        {
            srLegado.horizontal = false;
            srLegado.vertical = false;
            srLegado.enabled = false;
        }

        GenerarMapa();
        ConstruirLeyenda();
    }

    // ─────────────────────────────────────────────────────────────────────
    // ── el ancho del hub de cada día se ajusta al texto real del título
    //     (p.ej. "Día 115 Fase de exclusividad" necesita más espacio que
    //     "Día 1"), para que nunca se corte ni se superponga ──────────────
    string NombreDia(int d)
    {
        return (nombresDias != null && d < nombresDias.Length && !string.IsNullOrEmpty(nombresDias[d]))
            ? nombresDias[d] : $"Día {d + 1}";
    }

    float EstimarAnchoTexto(string texto, int fontSize)
    {
        if (string.IsNullOrEmpty(texto)) return 0f;
        return texto.Length * fontSize * 0.62f;
    }

    float AnchoHubBaseParaDia(string nomDia)
    {
        float anchoTexto = EstimarAnchoTexto(nomDia, fsHub) + padTarjeta * 2f;
        return Mathf.Max(anchoHub, anchoTexto);
    }

    // ─────────────────────────────────────────────────────────────────────
    void GenerarMapa()
    {
        GameManager gm = GameManager.Instance;
        TipoEleccion[] his = gm.HistorialElecciones;
        string[] txt = gm.HistorialTextos;

        float e = Mathf.Max(0.05f, escalaGlobal);

        // Todas las medidas escaladas
        float _gapCol = gapColumnas * e;
        float _margenIzq = margenIzquierdo * e;
        float _margenDer = margenDerecho * e;
        float _altoHub = altoHub * e;
        float _anchoMom = anchoMomento * e;
        float _altoMom = altoMomento * e;
        float _anchoDes = anchoDesenlace * e;
        float _altoDes = altoDesenlace * e;
        float _gapHubMom = gapHubMomentos * e;
        float _gapMom = gapEntreMomentos * e;
        float _grosor = Mathf.Max(2f, grosorLinea * e);
        float _pad = padTarjeta * e;
        float _anchoCol = anchoColumna * e;

        int _fsHub = Mathf.Max(10, Mathf.RoundToInt(fsHub * e));
        int _fsTit = Mathf.Max(9, Mathf.RoundToInt(fsTitMom * e));
        int _fsCpo = Mathf.Max(8, Mathf.RoundToInt(fsCpoMom * e));
        int _fsTDes = Mathf.Max(10, Mathf.RoundToInt(fsTitDes * e));
        int _fsCDes = Mathf.Max(9, Mathf.RoundToInt(fsCpoDes * e));

        // ── PASO 1: calcular TODAS las posiciones X (sin crear nada aún) ──
        float[] _anchoHubs = new float[GameManager.TOTAL_DIAS];
        float[] _colWidth = new float[GameManager.TOTAL_DIAS];
        float[] _xCols = new float[GameManager.TOTAL_DIAS];
        for (int d = 0; d < GameManager.TOTAL_DIAS; d++)
        {
            _anchoHubs[d] = AnchoHubBaseParaDia(NombreDia(d)) * e;
            _colWidth[d] = Mathf.Max(_anchoCol, _anchoHubs[d]);
        }

        float xCursor = _margenIzq;
        for (int d = 0; d < GameManager.TOTAL_DIAS; d++)
        {
            _xCols[d] = xCursor + _colWidth[d] * 0.5f;
            xCursor += _colWidth[d] + _gapCol;
        }

        float xDesRaw = xCursor + _anchoDes * 0.5f;
        float anchoTotal = xDesRaw + _anchoDes * 0.5f + _margenDer;

        // ── PASO 2: centrar — restar la mitad del ancho total a cada X ────
        // (esto es lo mismo que "startX = -(separacionHorizontal * 5.5f)"
        //  del script viejo: todo el mapa queda construido simétrico
        //  alrededor de x=0, así el Contenedor (centrado en su panel) lo
        //  muestra siempre centrado, sin importar la resolución) ──────────
        float mitad = anchoTotal * 0.5f;
        for (int d = 0; d < GameManager.TOTAL_DIAS; d++)
            _xCols[d] -= mitad;
        float xDes = xDesRaw - mitad;

        // Centrado vertical: el bloque (hub + 3 momentos) centrado en 0
        float bloqueH = _altoHub + _gapHubMom
                      + GameManager.DECISIONES_POR_DIA * _altoMom
                      + (GameManager.DECISIONES_POR_DIA - 1) * _gapMom;
        float _yHub = bloqueH * 0.5f - _altoHub * 0.5f;
        float _yPrimMom = _yHub - _altoHub * 0.5f - _gapHubMom - _altoMom * 0.5f;

        // ── PASO 3: recién ahora se crean las tarjetas y líneas ───────────
        for (int d = 0; d < GameManager.TOTAL_DIAS; d++)
        {
            float xCol = _xCols[d];
            string nomDia = NombreDia(d);

            CrearTarjeta(new Vector2(xCol, _yHub),
                _anchoHubs[d], _altoHub, colorDia,
                nomDia, null, _fsHub, _fsCpo, _pad);

            if (d > 0)
            {
                CrearLinea(new Vector2(_xCols[d - 1] + _anchoHubs[d - 1] * 0.5f, _yHub),
                           new Vector2(xCol - _anchoHubs[d] * 0.5f, _yHub), _grosor);
            }

            for (int m = 0; m < GameManager.DECISIONES_POR_DIA; m++)
            {
                int i = d * GameManager.DECISIONES_POR_DIA + m;
                if (i >= GameManager.TOTAL_MOMENTOS) break;

                float yMom = _yPrimMom - m * (_altoMom + _gapMom);

                string nomMom = (nombresMomentos != null && i < nombresMomentos.Length
                                 && !string.IsNullOrEmpty(nombresMomentos[i]))
                    ? nombresMomentos[i] : $"M{i + 1}";
                string elec = (txt != null && i < txt.Length) ? txt[i] : "";

                CrearTarjeta(new Vector2(xCol, yMom),
                    _anchoMom, _altoMom,
                    ColorSegunEleccion(his[i]),
                    nomMom, elec, _fsTit, _fsCpo, _pad);

                if (m == 0)
                    CrearLinea(new Vector2(xCol, _yHub - _altoHub * 0.5f),
                               new Vector2(xCol, yMom + _altoMom * 0.5f), _grosor);
                else
                    CrearLinea(new Vector2(xCol, yMom + _altoMom * 0.5f + _gapMom),
                               new Vector2(xCol, yMom + _altoMom * 0.5f), _grosor);
            }
        }

        CrearLinea(new Vector2(_xCols[GameManager.TOTAL_DIAS - 1] + _anchoHubs[GameManager.TOTAL_DIAS - 1] * 0.5f, _yHub),
                   new Vector2(xDes - _anchoDes * 0.5f, _yHub), _grosor);
        CrearTarjetaDesenlace(new Vector2(xDes, _yHub), _anchoDes, _altoDes, _fsTDes, _fsCDes, _pad);

        if (mostrarDebugEnPantalla)
        {
            MostrarDebugEnPantalla(
                $"escalaGlobal: {e:F3}\n" +
                $"anchoTotal (mapa): {anchoTotal:F0}\n" +
                $"contenedor: pivot/anchor centrado, anchoredPosition {contenedor.anchoredPosition}\n" +
                $"Screen: {Screen.width} × {Screen.height}"
            );
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    void CrearTarjetaDesenlace(Vector2 pos, float ancho, float alto, int fsTit, int fsCpo, float pad)
    {
        GameManager gm = GameManager.Instance;
        Color color = gm.EsFinal1 ? colorVerde : colorRojo;
        string titulo = gm.ObtenerTituloFinal();
        string cuerpo = gm.ObtenerMensajeFinal();

        GameObject go = CrearTarjeta(pos, ancho, alto, color, titulo, cuerpo, fsTit, fsCpo, pad);
        Outline ol = go.AddComponent<Outline>();
        ol.effectColor = new Color(1f, 1f, 1f, 0.9f);
        ol.effectDistance = new Vector2(2.5f, -2.5f);
    }

    // ─────────────────────────────────────────────────────────────────────
    GameObject CrearTarjeta(Vector2 pos, float ancho, float alto,
                             Color colorFondo, string titulo, string cuerpo,
                             int fsTit, int fsCpo, float pad)
    {
        GameObject go = Instantiate(prefabNodo, contenedor);
        RectTransform rt = go.GetComponent<RectTransform>();

        // Forzamos el anchor/pivot al centro, sin depender de cómo venga
        // armado el prefab — así 'pos' siempre se mide desde el centro
        // del Contenedor, igual que en el script viejo.
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(ancho, alto);

        Image img = go.GetComponent<Image>();
        if (img != null) img.color = colorFondo;

        Text tBase = go.GetComponentInChildren<Text>();
        if (tBase != null) tBase.gameObject.SetActive(false);

        bool conCuerpo = !string.IsNullOrEmpty(cuerpo);
        Vector2 aMinTit = conCuerpo ? new Vector2(0f, 1f - fraccionTitulo) : Vector2.zero;

        Texto(rt, "Titulo", aMinTit, Vector2.one,
              pad, titulo, fsTit, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter, false);

        if (conCuerpo)
        {
            float topCpo = 1f - fraccionTitulo - 0.03f;
            Texto(rt, "Cuerpo", Vector2.zero, new Vector2(1f, topCpo),
                  pad, cuerpo, fsCpo,
                  new Color(1f, 1f, 1f, 0.95f), FontStyle.Normal, TextAnchor.UpperCenter, true);
        }
        return go;
    }

    // ─────────────────────────────────────────────────────────────────────
    void Texto(RectTransform padre, string nombre,
               Vector2 aMin, Vector2 aMax, float pad, string texto,
               int fs, Color col, FontStyle estilo, TextAnchor align, bool wrap)
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
    void CrearLinea(Vector2 desde, Vector2 hasta, float grosor)
    {
        if (prefabLinea == null) return;
        GameObject go = Instantiate(prefabLinea, contenedor);
        RectTransform rt = go.GetComponent<RectTransform>();

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        Vector2 dir = hasta - desde;
        float dist = dir.magnitude;
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rt.anchoredPosition = desde + dir * 0.5f;
        rt.sizeDelta = new Vector2(dist, grosor);
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
            Debug.LogWarning("[MapaDecisiones] Asigna 'leyendaRaiz'.");
            return;
        }

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

        GameObject bg = new GameObject("Bg");
        bg.transform.SetParent(leyendaRaiz, false);
        RectTransform rtBg = bg.AddComponent<RectTransform>();
        rtBg.anchorMin = Vector2.zero; rtBg.anchorMax = Vector2.one;
        rtBg.offsetMin = Vector2.zero; rtBg.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 0.80f);

        float y = -pY;
        LeyTxt(leyendaRaiz, "Tit", new Vector2(pX, y),
               new Vector2(leyendaRaiz.sizeDelta.x - pX * 2f, titH),
               "Tipos de decisión:", 15, FontStyle.Bold, Color.white);
        y -= (titH + 8f);

        for (int i = 0; i < labels.Length; i++)
        {
            GameObject sq = new GameObject($"Sq{i}");
            sq.transform.SetParent(leyendaRaiz, false);
            RectTransform rtSq = sq.AddComponent<RectTransform>();
            rtSq.anchorMin = new Vector2(0, 1); rtSq.anchorMax = new Vector2(0, 1);
            rtSq.pivot = new Vector2(0, 1);
            rtSq.anchoredPosition = new Vector2(pX, y);
            rtSq.sizeDelta = new Vector2(cuad, cuad);
            sq.AddComponent<Image>().color = cols[i];

            LeyTxt(leyendaRaiz, $"L{i}",
                   new Vector2(pX + cuad + 7f, y + 1f),
                   new Vector2(leyendaRaiz.sizeDelta.x - pX - cuad - 10f, cuad + 2f),
                   labels[i], 13, FontStyle.Normal, Color.white);
            y -= (rowH + 5f);
        }
    }

    void LeyTxt(RectTransform p, string nom, Vector2 pos, Vector2 tam,
                string txt, int fs, FontStyle est, Color col)
    {
        GameObject obj = new GameObject(nom);
        obj.transform.SetParent(p, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos; rt.sizeDelta = tam;
        Text t = obj.AddComponent<Text>();
        t.text = txt;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fs; t.fontStyle = est; t.color = col;
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

    // ─────────────────────────────────────────────────────────────────────
    // ── overlay de texto simple, arriba a la izquierda, para leer valores
    //     de diagnóstico directamente en un BUILD (sin consola) ───────────
    void MostrarDebugEnPantalla(string info)
    {
        Canvas canvasRaiz = contenedor != null ? contenedor.GetComponentInParent<Canvas>() : null;
        if (canvasRaiz == null) { Debug.Log("[MapaDecisiones][Debug] " + info); return; }

        Transform existente = canvasRaiz.transform.Find("__DebugMapaDecisiones");
        GameObject go = existente != null ? existente.gameObject : new GameObject("__DebugMapaDecisiones");
        go.transform.SetParent(canvasRaiz.transform, false);
        go.transform.SetAsLastSibling();

        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(10f, -10f);
        rt.sizeDelta = new Vector2(700f, 160f);

        Image bg = go.GetComponent<Image>();
        if (bg == null) bg = go.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f);

        Text t = go.GetComponentInChildren<Text>();
        if (t == null)
        {
            GameObject txtGo = new GameObject("Texto");
            txtGo.transform.SetParent(go.transform, false);
            RectTransform trt = txtGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(8f, 8f); trt.offsetMax = new Vector2(-8f, -8f);
            t = txtGo.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 18;
            t.color = Color.yellow;
            t.alignment = TextAnchor.UpperLeft;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
        }
        t.text = info;
    }
}
