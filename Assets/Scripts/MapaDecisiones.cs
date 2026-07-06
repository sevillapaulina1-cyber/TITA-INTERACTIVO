using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MapaDecisiones v5 — Auto-escala para llenar el espacio disponible del Viewport.
///
/// LAYOUT: columnas verticales por día, scroll horizontal.
///   [Día X]   [Día X]   [Día X]   [Día X]   [DESENLACE]
///    [M1]      [M4]      [M7]      [M10]
///    [M2]      [M5]      [M8]      [M11]
///    [M3]      [M6]      [M9]      [M12]
///
/// El mapa calcula automáticamente una escala para llenar el ancho Y el alto
/// del Viewport del ScrollRect, sin necesidad de ajustar valores manualmente.
///
/// SETUP EN UNITY:
///   ScrollRect: horizontal=true, vertical=false
///   Contenedor (Content): pivot(0,0.5), anchor left-stretch — el script lo fuerza por código.
///   NO pongas Content Size Fitter en el Contenedor.
///   LeyendaRaiz: RectTransform hermano del ScrollRect (fuera de él).
/// </summary>
public class MapaDecisiones : MonoBehaviour
{
    // ── Referencias ───────────────────────────────────────────────────────
    [Header("── Referencias ────────────────────────")]
    public RectTransform contenedor;
    public ScrollRect scrollRect;
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

    // ── Layout BASE (se escalan automáticamente para llenar el Viewport) ──
    [Header("── Layout base (se auto-escalan) ────────")]
    [Tooltip("Ancho de cada columna de día")]
    public float anchoColumna = 220f;
    [Tooltip("Espacio horizontal entre columnas")]
    public float gapColumnas = 55f;
    [Tooltip("Margen izquierdo y derecho")]
    public float margenIzquierdo = 50f;
    public float margenDerecho = 50f;
    [Tooltip("Espacio entre la base del hub y la cima del primer momento")]
    public float gapHubMomentos = 20f;
    [Tooltip("Espacio entre tarjetas de momento consecutivas")]
    public float gapEntreMomentos = 14f;
    public float grosorLinea = 3f;

    // ── Tamaños BASE de tarjetas ──────────────────────────────────────────
    [Header("── Tamaños base (se auto-escalan) ────────")]
    public float anchoHub = 160f;
    public float altoHub = 52f;
    public float anchoMomento = 190f;
    public float altoMomento = 130f;
    public float anchoDesenlace = 310f;
    public float altoDesenlace = 280f;

    // ── Texto BASE ────────────────────────────────────────────────────────
    [Header("── Texto base (se auto-escalan) ─────────")]
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

    // ─────────────────────────────────────────────────────────────────────
    [Header("── Override manual (usa si auto-escala falla) ──")]
    [Tooltip("Si > 0, usa este ancho en lugar del Viewport real. Ponlo igual al ancho de tu ScrollRect en el Inspector.")]
    public float overrideAnchoViewport = 0f;
    [Tooltip("Si > 0, usa este alto en lugar del Viewport real. Ponlo igual al alto de tu ScrollRect en el Inspector.")]
    public float overrideAltoViewport = 0f;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[MapaDecisiones] GameManager no encontrado.");
            return;
        }

        if (scrollRect == null && contenedor != null)
            scrollRect = contenedor.GetComponentInParent<ScrollRect>();

        if (contenedor != null)
        {
            contenedor.pivot = new Vector2(0f, 0.5f);
            contenedor.anchorMin = new Vector2(0f, 0f);
            contenedor.anchorMax = new Vector2(0f, 1f);
            contenedor.offsetMin = Vector2.zero;
            contenedor.offsetMax = Vector2.zero;
        }

        StartCoroutine(GenerarConEscalaCO());
        ConstruirLeyenda();
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator GenerarConEscalaCO()
    {
        // Si hay override manual, úsalo directamente sin esperar
        if (overrideAnchoViewport > 10f && overrideAltoViewport > 10f)
        {
            GenerarMapa(CalcularEscala(overrideAnchoViewport, overrideAltoViewport),
                        overrideAnchoViewport, overrideAltoViewport);
            yield break;
        }

        // Esperar hasta 60 frames a que el Viewport tenga tamaño real
        RectTransform viewport = scrollRect != null ? scrollRect.viewport : null;
        float vpW = 0f, vpH = 0f;
        int intentos = 0;
        while (intentos < 60)
        {
            yield return null;
            intentos++;
            if (viewport != null)
            {
                vpW = viewport.rect.width;
                vpH = viewport.rect.height;
            }
            else if (scrollRect != null)
            {
                // Fallback: usar el RectTransform del propio ScrollRect
                RectTransform rtSR = scrollRect.GetComponent<RectTransform>();
                if (rtSR != null) { vpW = rtSR.rect.width; vpH = rtSR.rect.height; }
            }
            if (vpW > 50f && vpH > 50f) break;
        }

        // Si después de 60 frames sigue en 0, usar Screen como último recurso
        if (vpW < 50f) vpW = Screen.width * 0.75f; // el ScrollRect suele ser ~75% del ancho
        if (vpH < 50f) vpH = Screen.height * 0.55f; // y ~55% del alto

        Debug.Log($"[MapaDecisiones] Viewport: {vpW}×{vpH} (intentos: {intentos})");
        GenerarMapa(CalcularEscala(vpW, vpH), vpW, vpH);
    }

    float CalcularEscala(float vpW, float vpH)
    {
        float mapaW = margenIzquierdo
                    + GameManager.TOTAL_DIAS * anchoColumna
                    + (GameManager.TOTAL_DIAS - 1) * gapColumnas
                    + gapColumnas + anchoDesenlace
                    + margenDerecho;

        float mapaH = altoHub + gapHubMomentos
                    + GameManager.DECISIONES_POR_DIA * altoMomento
                    + (GameManager.DECISIONES_POR_DIA - 1) * gapEntreMomentos
                    + 30f;

        float escala = Mathf.Min(vpW / mapaW, vpH / mapaH);
        return Mathf.Clamp(escala, 0.4f, 3.0f);
    }

    // ─────────────────────────────────────────────────────────────────────
    void GenerarMapa(float e, float vpW, float vpH)
    {
        GameManager gm = GameManager.Instance;
        TipoEleccion[] his = gm.HistorialElecciones;
        string[] txt = gm.HistorialTextos;

        // Todas las medidas escaladas
        float _anchoCol = anchoColumna * e;
        float _gapCol = gapColumnas * e;
        float _margenIzq = margenIzquierdo * e;
        float _margenDer = margenDerecho * e;
        float _anchoHub = anchoHub * e;
        float _altoHub = altoHub * e;
        float _anchoMom = anchoMomento * e;
        float _altoMom = altoMomento * e;
        float _anchoDes = anchoDesenlace * e;
        float _altoDes = altoDesenlace * e;
        float _gapHubMom = gapHubMomentos * e;
        float _gapMom = gapEntreMomentos * e;
        float _grosor = Mathf.Max(2f, grosorLinea * e);
        float _pad = padTarjeta * e;

        int _fsHub = Mathf.Max(10, Mathf.RoundToInt(fsHub * e));
        int _fsTit = Mathf.Max(9, Mathf.RoundToInt(fsTitMom * e));
        int _fsCpo = Mathf.Max(8, Mathf.RoundToInt(fsCpoMom * e));
        int _fsTDes = Mathf.Max(10, Mathf.RoundToInt(fsTitDes * e));
        int _fsCDes = Mathf.Max(9, Mathf.RoundToInt(fsCpoDes * e));

        // Centrado vertical: el bloque (hub + 3 momentos) centrado en el viewport
        float bloqueH = _altoHub + _gapHubMom
                      + GameManager.DECISIONES_POR_DIA * _altoMom
                      + (GameManager.DECISIONES_POR_DIA - 1) * _gapMom;
        // yHub: posición Y del centro del hub medido desde el centro del contenedor
        float _yHub = bloqueH * 0.5f - _altoHub * 0.5f;
        float _yPrimMom = _yHub - _altoHub * 0.5f - _gapHubMom - _altoMom * 0.5f;

        for (int d = 0; d < GameManager.TOTAL_DIAS; d++)
        {
            float xCol = _margenIzq + d * (_anchoCol + _gapCol) + _anchoCol * 0.5f;

            string nomDia = (nombresDias != null && d < nombresDias.Length && !string.IsNullOrEmpty(nombresDias[d]))
                ? nombresDias[d] : $"Día {d + 1}";

            CrearTarjeta(new Vector2(xCol, _yHub),
                _anchoHub, _altoHub, colorDia,
                nomDia, null, _fsHub, _fsCpo, _pad);

            if (d > 0)
            {
                float xPrev = _margenIzq + (d - 1) * (_anchoCol + _gapCol) + _anchoCol * 0.5f;
                CrearLinea(new Vector2(xPrev + _anchoHub * 0.5f, _yHub),
                           new Vector2(xCol - _anchoHub * 0.5f, _yHub), _grosor);
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

        // Tarjeta de desenlace
        float xUlt = _margenIzq + (GameManager.TOTAL_DIAS - 1) * (_anchoCol + _gapCol) + _anchoCol * 0.5f;
        float xDes = xUlt + _anchoCol * 0.5f + _gapCol + _anchoDes * 0.5f;

        CrearLinea(new Vector2(xUlt + _anchoHub * 0.5f, _yHub),
                   new Vector2(xDes - _anchoDes * 0.5f, _yHub), _grosor);
        CrearTarjetaDesenlace(new Vector2(xDes, _yHub), _anchoDes, _altoDes, _fsTDes, _fsCDes, _pad);

        // Ajustar el Contenedor y centrar si cabe
        float anchoTotal = xDes + _anchoDes * 0.5f + _margenDer;
        float offsetX = anchoTotal < vpW ? (vpW - anchoTotal) * 0.5f : 0f;

        if (contenedor != null)
        {
            contenedor.sizeDelta = new Vector2(Mathf.Max(anchoTotal, vpW), contenedor.sizeDelta.y);
            contenedor.anchoredPosition = new Vector2(offsetX, 0f);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    void CrearTarjetaDesenlace(Vector2 pos, float ancho, float alto, int fsTit, int fsCpo, float pad)
    {
        GameManager gm = GameManager.Instance;
        Color color = gm.EsFinal1 ? colorVerde : colorRojo;
        string titulo = gm.ObtenerTituloFinal();
        // Sin puntos — solo el mensaje de reflexión
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

        // Fondo
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
}