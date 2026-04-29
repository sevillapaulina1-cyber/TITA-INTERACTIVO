using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Genera el mapa visual de decisiones en la pantalla de retroalimentación.
/// Muestra los 12 momentos conectados con líneas, coloreados según la elección.
///
/// SETUP EN UNITY:
///   1. Crea un GameObject vacío en la escena de final → Add Component → MapaDecisiones
///   2. En el Canvas crea un panel "PanelMapa" vacío (fondo oscuro, pantalla completa)
///   3. Asigna los campos en el Inspector
///
/// COLORES:
///   Verde  → decisión protectora
///   Rojo   → decisión vulnerable
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
    public Color colorVerde  = new Color(0.11f, 0.73f, 0.33f);
    public Color colorRojo   = new Color(0.85f, 0.15f, 0.15f);
    public Color colorGris   = new Color(0.6f,  0.6f,  0.6f);
    public Color colorLinea  = new Color(1f,    1f,    1f, 0.3f);

    [Header("── Layout ───────────────────────────────")]
    public float separacionHorizontal = 90f;    // distancia entre nodos
    public float amplitudOnda         = 40f;    // altura de la onda
    public float tamanoNodo           = 50f;    // diámetro del círculo

    [Header("── Nombres de momentos (opcional) ──────")]
    [Tooltip("12 nombres cortos para cada momento. Déjalos vacíos para usar números.")]
    public string[] nombresMomentos = {
        "Contacto", "Juego", "Cierre",
        "Reencuentro", "Emoción", "Vínculo",
        "Rutina", "Contexto", "Confianza",
        "Canal", "Secreto", "Encuentro"
    };

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

        // Punto de inicio centrado a la izquierda
        float startX = -(separacionHorizontal * 5.5f);

        for (int i = 0; i < 12; i++)
        {
            float x = startX + (i * separacionHorizontal);
            // Onda sinusoidal para el eje Y
            float y = Mathf.Sin(i * Mathf.PI / 3f) * amplitudOnda;

            // ── Nodo ────────────────────────────────────────────────────
            GameObject nodo = Instantiate(prefabNodo, contenedor);
            RectTransform rt = nodo.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta        = new Vector2(tamanoNodo, tamanoNodo);

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
                textoNodo.fontSize  = 11;
                textoNodo.color     = Color.white;
                textoNodo.alignment = TextAnchor.MiddleCenter;
            }

            // Etiqueta del día encima de los nodos 1, 4, 7, 10
            if (i % 3 == 0)
                CrearEtiquetaDia(contenedor, x, y + tamanoNodo, i / 3 + 1);

            // ── Línea conectora ──────────────────────────────────────────
            if (i < 11)
            {
                float xSig = startX + ((i + 1) * separacionHorizontal);
                float ySig = Mathf.Sin((i + 1) * Mathf.PI / 3f) * amplitudOnda;

                CrearLinea(contenedor, new Vector2(x, y), new Vector2(xSig, ySig));
            }
        }

        // ── Leyenda ──────────────────────────────────────────────────────
        CrearLeyenda();
    }

    // ─────────────────────────────────────────────────────────────────────
    void CrearLinea(RectTransform padre, Vector2 desde, Vector2 hasta)
    {
        if (prefabLinea == null) return;

        GameObject linea = Instantiate(prefabLinea, padre);
        RectTransform rt = linea.GetComponent<RectTransform>();

        Vector2 direccion = hasta - desde;
        float   distancia = direccion.magnitude;
        float   angulo    = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;

        rt.anchoredPosition = desde + direccion * 0.5f;
        rt.sizeDelta        = new Vector2(distancia, 3f);
        rt.localRotation    = Quaternion.Euler(0, 0, angulo);

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
        rt.sizeDelta        = new Vector2(80f, 20f);

        Text texto = etiqueta.AddComponent<Text>();
        texto.text      = $"Día {dia}";
        texto.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        texto.fontSize  = 12;
        texto.color     = new Color(1f, 1f, 1f, 0.6f);
        texto.alignment = TextAnchor.MiddleCenter;
    }

    // ─────────────────────────────────────────────────────────────────────
    void CrearLeyenda()
    {
        // Crea 3 entradas de leyenda en la esquina inferior izquierda
        string[] labels = { "Decisión protectora", "Decisión ambigua", "Decisión vulnerable" };
        Color[]  colors = { colorVerde, colorGris, colorRojo };

        for (int i = 0; i < 3; i++)
        {
            // Punto de color
            GameObject punto = new GameObject($"LeyendaPunto{i}");
            punto.transform.SetParent(contenedor, false);
            RectTransform rtP = punto.AddComponent<RectTransform>();
            rtP.anchorMin       = new Vector2(0, 0);
            rtP.anchorMax       = new Vector2(0, 0);
            rtP.pivot           = new Vector2(0, 0);
            rtP.anchoredPosition = new Vector2(20f, 20f + i * 24f);
            rtP.sizeDelta       = new Vector2(14f, 14f);
            Image imgP = punto.AddComponent<Image>();
            imgP.color = colors[i];

            // Texto
            GameObject textoObj = new GameObject($"LeyendaTexto{i}");
            textoObj.transform.SetParent(contenedor, false);
            RectTransform rtT = textoObj.AddComponent<RectTransform>();
            rtT.anchorMin       = new Vector2(0, 0);
            rtT.anchorMax       = new Vector2(0, 0);
            rtT.pivot           = new Vector2(0, 0);
            rtT.anchoredPosition = new Vector2(40f, 18f + i * 24f);
            rtT.sizeDelta       = new Vector2(180f, 18f);
            Text txt = textoObj.AddComponent<Text>();
            txt.text      = labels[i];
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize  = 12;
            txt.color     = Color.white;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    Color ColorSegunEleccion(TipoEleccion tipo)
    {
        switch (tipo)
        {
            case TipoEleccion.Verde:  return colorVerde;
            case TipoEleccion.Rojo:   return colorRojo;
            default:                  return colorGris;
        }
    }
}
