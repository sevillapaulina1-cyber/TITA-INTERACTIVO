using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Genera el mapa visual de decisiones en la pantalla de retroalimentación.
/// Muestra los 12 momentos conectados con una curva suave, coloreados según la elección,
/// y con el texto exacto que el jugador escogió debajo de cada nodo.
///
/// SETUP EN UNITY:
///   1. Crea un GameObject vacío en la escena de final → Add Component → MapaDecisiones
///   2. En el Canvas crea un panel "PanelMapa" vacío (fondo oscuro, pantalla completa)
///   3. Asigna los campos en el Inspector
///   4. Si el mapa no entra en pantalla, mete el "contenedor" dentro de un ScrollRect
///      horizontal, o reduce separacionHorizontal/amplitudOnda desde el Inspector.
///
/// COLORES:
///   Verde  → decisión de confianza
///   Rojo   → decisión riesgosa
///   Gris   → decisión neutra
/// </summary>
public class MapaDecisiones : MonoBehaviour
{
    [Header("── Contenedor ──────────────────────────")]
    public RectTransform contenedor;    // Panel donde se dibuja el mapa

    [Header("── Prefabs ─────────────────────────────")]
    public GameObject prefabNodo;       // Círculo con Text para cada momento
    public GameObject prefabLinea;      // Image horizontal que conecta nodos

    [Header("── Colores ─────────────────────────────")]
    public Color colorVerde = new Color(0.11f, 0.73f, 0.33f);
    public Color colorRojo = new Color(0.85f, 0.15f, 0.15f);
    public Color colorGris = new Color(0.6f, 0.6f, 0.6f);
    public Color colorLinea = new Color(1f, 1f, 1f, 0.35f);

    [Header("── Layout (mapa más grande) ────────────")]
    [Tooltip("Distancia horizontal entre nodos")]
    public float separacionHorizontal = 130f;   // antes 90f
    [Tooltip("Altura de la onda. A mayor valor, curvas más pronunciadas")]
    public float amplitudOnda = 95f;    // antes 40f
    [Tooltip("Diámetro del círculo de cada nodo")]
    public float tamanoNodo = 64f;    // antes 50f
    [Tooltip("Grosor de la línea/curva conectora")]
    public float grosorLinea = 4f;

    [Header("── Suavidad de la curva ────────────────")]
    [Tooltip("Cuántos segmentos pequeños se dibujan entre un nodo y el siguiente. Más alto = curva más suave.")]
    [Range(2, 24)]
    public int segmentosPorTramo = 12;

    [Header("── Nombres de momentos (opcional) ──────")]
    [Tooltip("12 nombres cortos para cada momento. Déjalos vacíos para usar números.")]
    public string[] nombresMomentos = {
        "Contacto", "Juego", "Cierre",
        "Reencuentro", "Emoción", "Vínculo",
        "Rutina", "Contexto", "Confianza",
        "Canal", "Secreto", "Encuentro"
    };

    [Header("── Texto de la elección del jugador ────")]
    [Tooltip("Muestra debajo de cada nodo el texto exacto que el jugador eligió en ese momento")]
    public bool mostrarTextoEleccion = true;
    [Tooltip("Tamaño de fuente del texto de elección")]
    public int tamanoFuenteEleccion = 11;
    [Tooltip("Ancho disponible para el texto de elección (se ajusta con salto de línea)")]
    public float anchoTextoEleccion = 150f;
    [Tooltip("Separación vertical extra entre el nodo y el texto de elección")]
    public float separacionTextoEleccion = 26f;
    public Color colorTextoEleccion = new Color(1f, 1f, 1f, 0.85f);

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
        TipoEleccion[] historial = GameManager.Instance.HistorialElecciones;
        string[] textos = GameManager.Instance.HistorialTextos;

        // Punto de inicio centrado a la izquierda
        float startX = -(separacionHorizontal * 5.5f);

        for (int i = 0; i < 12; i++)
        {
            Vector2 pos = PuntoEnCurva(startX, i, 0f);
            float x = pos.x;
            float y = pos.y;

            // ── Nodo ────────────────────────────────────────────────────
            GameObject nodo = Instantiate(prefabNodo, contenedor);
            RectTransform rt = nodo.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(tamanoNodo, tamanoNodo);

            // Color según elección
            Image imgNodo = nodo.GetComponent<Image>();
            if (imgNodo != null)
                imgNodo.color = ColorSegunEleccion(historial[i]);

            // Número o nombre del momento
            Text textoNodo = nodo.GetComponentInChildren<Text>();
            if (textoNodo != null)
            {
                textoNodo.text = (nombresMomentos != null && i < nombresMomentos.Length && !string.IsNullOrEmpty(nombresMomentos[i]))
                    ? nombresMomentos[i]
                    : $"{i + 1}";
                textoNodo.fontSize = 11;
                textoNodo.color = Color.white;
                textoNodo.alignment = TextAnchor.MiddleCenter;
            }

            // Etiqueta del día encima de los nodos 1, 4, 7, 10
            if (i % 3 == 0)
                CrearEtiquetaDia(contenedor, x, y + tamanoNodo, i / 3 + 1);

            // ── Texto de la elección exacta del jugador ───────────────────
            if (mostrarTextoEleccion && textos != null && i < textos.Length)
                CrearTextoEleccion(contenedor, x, y, textos[i]);

            // ── Curva conectora suave hacia el siguiente nodo ─────────────
            if (i < 11)
                CrearCurvaSuave(contenedor, startX, i);
        }

        // ── Leyenda ──────────────────────────────────────────────────────
        CrearLeyenda();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Devuelve la posición (x,y) en la curva sinusoidal para un índice fraccionario.
    // indiceBase = nodo de partida del tramo, t = 0..1 dentro de ese tramo.
    Vector2 PuntoEnCurva(float startX, int indiceBase, float t)
    {
        float indice = indiceBase + t;
        float x = startX + indice * separacionHorizontal;
        float y = Mathf.Sin(indice * Mathf.PI / 3f) * amplitudOnda;
        return new Vector2(x, y);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Dibuja varios segmentos cortos siguiendo la curva real entre el nodo i e i+1,
    // en vez de una sola línea recta. Resultado: curva pronunciada pero suave.
    void CrearCurvaSuave(RectTransform padre, float startX, int i)
    {
        int pasos = Mathf.Max(2, segmentosPorTramo);
        Vector2 anterior = PuntoEnCurva(startX, i, 0f);

        for (int p = 1; p <= pasos; p++)
        {
            float t = (float)p / pasos;
            Vector2 actual = PuntoEnCurva(startX, i, t);
            CrearLinea(padre, anterior, actual);
            anterior = actual;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    void CrearLinea(RectTransform padre, Vector2 desde, Vector2 hasta)
    {
        if (prefabLinea == null) return;

        GameObject linea = Instantiate(prefabLinea, padre);
        RectTransform rt = linea.GetComponent<RectTransform>();

        Vector2 direccion = hasta - desde;
        float distancia = direccion.magnitude;
        float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;

        rt.anchoredPosition = desde + direccion * 0.5f;
        rt.sizeDelta = new Vector2(distancia, grosorLinea);
        rt.localRotation = Quaternion.Euler(0, 0, angulo);

        Image imgLinea = linea.GetComponent<Image>();
        if (imgLinea != null) imgLinea.color = colorLinea;

        // Mandar la línea detrás de los nodos
        linea.transform.SetAsFirstSibling();
    }

    // ─────────────────────────────────────────────────────────────────────
    void CrearEtiquetaDia(RectTransform padre, float x, float y, int dia)
    {
        GameObject etiqueta = new GameObject($"EtiquetaDia{dia}");
        etiqueta.transform.SetParent(padre, false);

        RectTransform rt = etiqueta.AddComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(x, y + 20f);
        rt.sizeDelta = new Vector2(80f, 20f);

        Text texto = etiqueta.AddComponent<Text>();
        texto.text = $"Día {dia}";
        texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        texto.fontSize = 12;
        texto.color = new Color(1f, 1f, 1f, 0.6f);
        texto.alignment = TextAnchor.MiddleCenter;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Crea, debajo de cada nodo, el texto exacto que el jugador eligió en ese momento.
    void CrearTextoEleccion(RectTransform padre, float x, float y, string texto)
    {
        if (string.IsNullOrEmpty(texto)) return;

        GameObject obj = new GameObject("TextoEleccion");
        obj.transform.SetParent(padre, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(x, y - (tamanoNodo * 0.5f) - separacionTextoEleccion);
        rt.sizeDelta = new Vector2(anchoTextoEleccion, 70f);

        Text txt = obj.AddComponent<Text>();
        txt.text = texto;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = tamanoFuenteEleccion;
        txt.color = colorTextoEleccion;
        txt.alignment = TextAnchor.UpperCenter;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
    }

    // ─────────────────────────────────────────────────────────────────────
    void CrearLeyenda()
    {
        // Crea 3 entradas de leyenda en la esquina inferior izquierda
        string[] labels = { "Decisión de confianza", "Decisión neutra", "Decisión riesgosa" };
        Color[] colors = { colorVerde, colorGris, colorRojo };

        for (int i = 0; i < 3; i++)
        {
            // Punto de color
            GameObject punto = new GameObject($"LeyendaPunto{i}");
            punto.transform.SetParent(contenedor, false);
            RectTransform rtP = punto.AddComponent<RectTransform>();
            rtP.anchorMin = new Vector2(0, 0);
            rtP.anchorMax = new Vector2(0, 0);
            rtP.pivot = new Vector2(0, 0);
            rtP.anchoredPosition = new Vector2(20f, 20f + i * 24f);
            rtP.sizeDelta = new Vector2(14f, 14f);
            Image imgP = punto.AddComponent<Image>();
            imgP.color = colors[i];

            // Texto
            GameObject textoObj = new GameObject($"LeyendaTexto{i}");
            textoObj.transform.SetParent(contenedor, false);
            RectTransform rtT = textoObj.AddComponent<RectTransform>();
            rtT.anchorMin = new Vector2(0, 0);
            rtT.anchorMax = new Vector2(0, 0);
            rtT.pivot = new Vector2(0, 0);
            rtT.anchoredPosition = new Vector2(40f, 18f + i * 24f);
            rtT.sizeDelta = new Vector2(200f, 18f);
            Text txt = textoObj.AddComponent<Text>();
            txt.text = labels[i];
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 12;
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
