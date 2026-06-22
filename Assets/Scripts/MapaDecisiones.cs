using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mapa CONCEPTUAL HORIZONTAL de decisiones para la pantalla de retroalimentación.
///
/// LAYOUT:
///   Los 4 días se distribuyen de izquierda a derecha como una línea de tiempo.
///   Cada día tiene un hub central (Día 1, Día 2…) y sus 3 momentos se ramifican
///   hacia ABAJO, como un mapa mental girado 90°. Al final (a la derecha de Día 4)
///   aparece una tarjeta de resumen con los puntos y el mensaje del desenlace.
///
/// SETUP EN UNITY:
///   1. Estructura recomendada en el Canvas:
///
///      Canvas
///      └── PanelRetro              (el panel activo de retroalimentación)
///          ├── ZonaSuperior        (textos de resumen, puntos, botón Reiniciar)
///          └── ScrollRect          ← componente ScrollRect
///              ├── Viewport        ← Mask + Image (del ScrollRect)
///              └── Contenedor      ← RectTransform; asignar como "Content" del ScrollRect
///                  └── MapaDecisionesGO  ← este script (contenedor = Contenedor)
///
///   2. ScrollRect: horizontal ✓, vertical ✗
///      Content Size Fitter en Contenedor: Horizontal = Preferred Size, Vertical = None
///      Anchor del Contenedor: izquierda-centro (left-center stretch), pivot (0, 0.5)
///
///   3. Este script recibe el mismo "Contenedor" RectTransform en el campo "contenedor".
///
/// COLORES:
///   Verde  → decisión de confianza
///   Rojo   → decisión riesgosa
///   Gris   → decisión neutra
/// </summary>
public class MapaDecisiones : MonoBehaviour
{
    [Header("── Contenedor (Content del ScrollRect) ──")]
    [Tooltip("El RectTransform marcado como Content en el ScrollRect horizontal")]
    public RectTransform contenedor;

    [Header("── Prefabs ─────────────────────────────")]
    public GameObject prefabNodo;   // Image (rectángulo redondeado) con Text hijo
    public GameObject prefabLinea;  // Image usada como segmento de línea

    [Header("── Colores ─────────────────────────────")]
    public Color colorVerde = new Color(0.11f, 0.73f, 0.33f);
    public Color colorRojo = new Color(0.85f, 0.15f, 0.15f);
    public Color colorGris = new Color(0.55f, 0.55f, 0.55f);
    public Color colorLinea = new Color(1f, 1f, 1f, 0.4f);
    public Color colorDia = new Color(0.14f, 0.42f, 0.48f);  // hub "Día N"

    [Header("── Layout horizontal ───────────────────")]
    [Tooltip("Distancia entre el centro de un hub de día y el siguiente (eje X)")]
    public float separacionEntreDias = 500f;
    [Tooltip("Distancia vertical entre el hub y sus tarjetas de momento (eje Y, hacia abajo)")]
    public float separacionVerticalMomentos = 190f;
    [Tooltip("Margen izquierdo antes del primer día")]
    public float margenIzquierdo = 140f;
    [Tooltip("Grosor de las líneas conectoras")]
    public float grosorLinea = 4f;

    [Header("── Tamaños de tarjetas ─────────────────")]
    public float anchoHubDia = 130f;
    public float altoHubDia = 70f;
    public float anchoTarjeta = 230f;
    public float altoTarjeta = 190f;
    public float anchoTarjetaFinal = 300f;
    public float altoTarjetaFinal = 200f;

    [Header("── Texto dentro de las tarjetas ─────────")]
    [Tooltip("Muestra la opción exacta que eligió el jugador dentro de cada tarjeta")]
    public bool mostrarTextoEleccion = true;
    [Tooltip("Padding interno entre borde de la tarjeta y el texto")]
    public float paddingInternoTarjeta = 16f;
    [Tooltip("Fracción de la altura de la tarjeta reservada para el título (0.25 = 25 %)")]
    [Range(0.15f, 0.5f)]
    public float alturaRelativaTitulo = 0.28f;
    [Tooltip("Espacio adicional entre título y cuerpo, como fracción de la altura")]
    [Range(0f, 0.12f)]
    public float separacionTituloCuerpo = 0.05f;
    public int tamanoFuenteTitulo = 15;
    public int tamanoFuenteEleccion = 13;
    public Color colorTitulo = Color.white;
    public Color colorTextoEleccion = new Color(1f, 1f, 1f, 0.95f);

    [Header("── Nombres ──────────────────────────────")]
    public string[] nombresMomentos = {
        "Contacto", "Juego", "Cierre",
        "Reencuentro", "Emoción", "Vínculo",
        "Rutina", "Contexto", "Confianza",
        "Canal", "Secreto", "Encuentro"
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
    }

    // ─────────────────────────────────────────────────────────────────────
    void GenerarMapa()
    {
        GameManager gm = GameManager.Instance;
        TipoEleccion[] historial = gm.HistorialElecciones;
        string[] textos = gm.HistorialTextos;

        // Y del hub de cada día: centrado en 0
        float yHub = 0f;

        for (int d = 0; d < GameManager.TOTAL_DIAS; d++)
        {
            float xDia = margenIzquierdo + d * separacionEntreDias;

            // ── Hub del día ────────────────────────────────────────────────
            string nombreDia = (nombresDias != null && d < nombresDias.Length && !string.IsNullOrEmpty(nombresDias[d]))
                ? nombresDias[d] : $"Día {d + 1}";

            CrearTarjeta(new Vector2(xDia, yHub), anchoHubDia, altoHubDia, colorDia, nombreDia, null);

            // ── Línea horizontal que conecta los hubs entre días ────────────
            if (d > 0)
            {
                float xDiaAnterior = margenIzquierdo + (d - 1) * separacionEntreDias;
                CrearLinea(
                    new Vector2(xDiaAnterior + anchoHubDia * 0.5f, yHub),
                    new Vector2(xDia - anchoHubDia * 0.5f, yHub));
            }

            // ── 3 tarjetas de momentos ramificadas hacia abajo ─────────────
            for (int m = 0; m < GameManager.DECISIONES_POR_DIA; m++)
            {
                int i = d * GameManager.DECISIONES_POR_DIA + m;
                if (i >= GameManager.TOTAL_MOMENTOS) break;

                // m=0 → justo debajo del hub, m=1 → más abajo, m=2 → aún más
                float yMomento = yHub - altoHubDia * 0.5f - (m + 0.5f) * separacionVerticalMomentos;
                Vector2 posMom = new Vector2(xDia, yMomento);

                string nombreMom = (nombresMomentos != null && i < nombresMomentos.Length && !string.IsNullOrEmpty(nombresMomentos[i]))
                    ? nombresMomentos[i] : $"Momento {i + 1}";

                string textoEleg = (textos != null && i < textos.Length) ? textos[i] : "";

                CrearTarjeta(posMom, anchoTarjeta, altoTarjeta,
                    ColorSegunEleccion(historial[i]), nombreMom,
                    mostrarTextoEleccion ? textoEleg : null);

                // Línea: del borde inferior del hub al borde superior de la tarjeta
                CrearLinea(
                    new Vector2(xDia, yHub - altoHubDia * 0.5f),
                    new Vector2(posMom.x, posMom.y + altoTarjeta * 0.5f));
            }
        }

        // ── Tarjeta final a la derecha del último día ──────────────────────
        float xFinal = margenIzquierdo + GameManager.TOTAL_DIAS * separacionEntreDias;
        float yFinal = yHub;

        // Línea desde el hub del último día hasta la tarjeta final
        float xUltimoDia = margenIzquierdo + (GameManager.TOTAL_DIAS - 1) * separacionEntreDias;
        CrearLinea(
            new Vector2(xUltimoDia + anchoHubDia * 0.5f, yFinal),
            new Vector2(xFinal - anchoTarjetaFinal * 0.5f, yFinal));

        CrearTarjetaFinal(new Vector2(xFinal, yFinal));

        // Ajustar el ancho del contenedor para que el ScrollRect sepa
        // hasta dónde tiene que scrollear
        float anchoTotal = xFinal + anchoTarjetaFinal * 0.5f + margenIzquierdo;
        if (contenedor != null)
            contenedor.sizeDelta = new Vector2(anchoTotal, contenedor.sizeDelta.y);

        // ── Leyenda ──────────────────────────────────────────────────────
        CrearLeyenda();
    }

    // ─────────────────────────────────────────────────────────────────────
    void CrearTarjetaFinal(Vector2 pos)
    {
        GameManager gm = GameManager.Instance;
        string titulo = gm.ObtenerTituloFinal();
        string cuerpo = $"Confianza: {gm.PuntosConfianza} pts\nRiesgo: {gm.PuntosRiesgo} pts\n\n{gm.ObtenerMensajeFinal()}";
        Color color = gm.EsFinal1 ? colorVerde : colorRojo;

        CrearTarjeta(pos, anchoTarjetaFinal, altoTarjetaFinal, color, titulo, cuerpo);
    }

    // ─────────────────────────────────────────────────────────────────────
    GameObject CrearTarjeta(Vector2 pos, float ancho, float alto,
                             Color colorFondo, string titulo, string cuerpo)
    {
        GameObject tarjeta = Instantiate(prefabNodo, contenedor);
        RectTransform rt = tarjeta.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(ancho, alto);

        Image img = tarjeta.GetComponent<Image>();
        if (img != null) img.color = colorFondo;

        // Desactivar el Text del prefab; creamos los nuestros con layout propio
        Text textoPrefab = tarjeta.GetComponentInChildren<Text>();
        if (textoPrefab != null) textoPrefab.gameObject.SetActive(false);

        bool tieneCuerpo = !string.IsNullOrEmpty(cuerpo);

        // Zona del título: ocupa la parte superior si hay cuerpo, o toda la tarjeta si no
        Vector2 anchorMinTitulo = tieneCuerpo ? new Vector2(0f, 1f - alturaRelativaTitulo) : Vector2.zero;
        CrearTextoHijo(rt, "Titulo", anchorMinTitulo, Vector2.one,
            paddingInternoTarjeta, titulo,
            tamanoFuenteTitulo, colorTitulo, FontStyle.Bold, TextAnchor.MiddleCenter, false);

        if (tieneCuerpo)
        {
            float topCuerpo = 1f - alturaRelativaTitulo - separacionTituloCuerpo;
            CrearTextoHijo(rt, "Cuerpo", Vector2.zero, new Vector2(1f, topCuerpo),
                paddingInternoTarjeta, cuerpo,
                tamanoFuenteEleccion, colorTextoEleccion, FontStyle.Normal, TextAnchor.UpperCenter, true);
        }

        return tarjeta;
    }

    // ─────────────────────────────────────────────────────────────────────
    void CrearTextoHijo(RectTransform padre, string nombre,
                         Vector2 anchorMin, Vector2 anchorMax,
                         float padding, string texto, int fontSize, Color color,
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

    // ─────────────────────────────────────────────────────────────────────
    void CrearLeyenda()
    {
        // Leyenda fija en la esquina inferior-izquierda del contenedor
        string[] labels = { "Decisión de confianza", "Decisión neutra", "Decisión riesgosa" };
        Color[] colors = { colorVerde, colorGris, colorRojo };

        for (int i = 0; i < 3; i++)
        {
            // Cuadrado de color
            GameObject punto = new GameObject($"LeyendaPunto{i}");
            punto.transform.SetParent(contenedor, false);
            RectTransform rtP = punto.AddComponent<RectTransform>();
            rtP.anchorMin = Vector2.zero;
            rtP.anchorMax = Vector2.zero;
            rtP.pivot = Vector2.zero;
            rtP.anchoredPosition = new Vector2(20f, 20f + i * 28f);
            rtP.sizeDelta = new Vector2(16f, 16f);
            punto.AddComponent<Image>().color = colors[i];

            // Texto de la leyenda
            GameObject textoObj = new GameObject($"LeyendaTexto{i}");
            textoObj.transform.SetParent(contenedor, false);
            RectTransform rtT = textoObj.AddComponent<RectTransform>();
            rtT.anchorMin = Vector2.zero;
            rtT.anchorMax = Vector2.zero;
            rtT.pivot = Vector2.zero;
            rtT.anchoredPosition = new Vector2(44f, 18f + i * 28f);
            rtT.sizeDelta = new Vector2(210f, 22f);
            Text txt = textoObj.AddComponent<Text>();
            txt.text = labels[i];
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 14;
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
