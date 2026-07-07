using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class TransicionDia : MonoBehaviour
{
    [Header("── UI: partes que RUEDAN (número día, número fecha, mes) ──")]
    [Tooltip("Solo el número de \"Día X\" (ej: \"1\", \"2\"...). Rueda hacia abajo al cambiar.")]
    public Text textoDiaNumero;
    [Tooltip("Solo el número de día del mes de la fecha (ej: \"20\", \"4\"...). Rueda al cambiar.")]
    public Text textoFechaNumero;
    [Tooltip("Solo el nombre del mes (ej: \"marzo\", \"abril\"...). Rueda al cambiar.")]
    public Text textoFechaMes;

    [Header("── UI: partes ESTÁTICAS (no ruedan, solo aparecen/desaparecen) ──")]
    [Tooltip("La palabra \"Día\" (se autocompleta con el prefijo detectado en nombreDia1 / nombresDias)")]
    public Text textoDiaEtiqueta;
    [Tooltip("La palabra \"de\" entre el número y el mes")]
    public Text textoFechaConector;
    [Tooltip("El resto de la fecha: \", 2026\" (coma + año)")]
    public Text textoFechaSufijo;

    public Image panelNegro;

    [Header("── NPCs (uno por día, en orden) ─────────")]
    [Tooltip("4 NPCs duplicados. Índice 0 = Día 1, 1 = Día 2, etc.")]
    public GameObject[] npcsPorDia;

    [Header("── Aviso \"Habla con SamuVR\" por día ─────")]
    [Tooltip("SistemaDialogo del momento de inicio de cada día. " +
             "Índice 0 = Día 1 (momento 1), 1 = Día 2 (momento 4), 2 = Día 3 (momento 7). " +
             "El índice 3 (Día 4) puede dejarse vacío — no muestra aviso.")]
    public SistemaDialogo[] dialogoInicioPorDia;
    [Tooltip("Segundos de espera tras el fade del Día 1 antes de mostrar el aviso " +
             "(debe coincidir aproximadamente con el tiempo visible del tutorial)")]
    public float delayAvisoDia1 = 20f;
    [Tooltip("Segundos de espera tras el fade de transición (días 2 y 3) antes de mostrar el aviso")]
    public float delayAvisoTransicion = 2f;

    [Header("── Spawns del jugador (uno por día) ──────")]
    [Tooltip("4 GameObjects vacíos con la posición de inicio de cada día")]
    public Transform[] spawnsPorDia;

    [Header("── Referencia al jugador ───────────────")]
    public Transform playerTransform;
    public MonoBehaviour firstPersonController;

    [Header("── Día 1 (intro al cargar la escena) ────")]
    [Tooltip("Muestra el fade de Día 1 al entrar desde la cinemática")]
    public bool mostrarIntroDia1 = true;
    public string nombreDia1 = "Día 1";
    public string fechaDia1 = "20 de marzo, 2026";

    [Header("── Fechas de transición (días 2–4) ───────")]
    [Tooltip("3 entradas: fecha al pasar al día 2, 3 y 4")]
    public string[] fechas = {
        "4 de abril, 2026",
        "2 de mayo, 2026",
        "20 de mayo, 2026"
    };

    [Tooltip("3 entradas: nombre al pasar al día 2, 3 y 4")]
    public string[] nombresDias = {
        "Día 2",
        "Día 3",
        "Día 4"
    };

    [Header("── Audio de transición ──────────────────")]
    [Tooltip("AudioSource para el sonido de transición de día (se crea automáticamente)")]
    public AudioSource fuenteTransicion;
    [Tooltip("Clip de sonido de transición (campanada, whoosh, etc.)")]
    public AudioClip clipTransicion;
    [Range(0f, 1f)]
    public float volumenTransicion = 0.9f;

    [Header("── Tiempos ─────────────────────────────")]
    public float duracionFadeIn = 1.0f;
    public float duracionTexto = 2.0f;
    public float duracionFadeOut = 1.0f;

    [Header("── Animación tipo \"odómetro\" (solo números y mes) ──")]
    [Tooltip("Distancia en píxeles que recorre cada pieza al rodar")]
    public float distanciaDeslizamiento = 40f;
    [Tooltip("Duración del rodado")]
    public float duracionAnimacionTexto = 0.5f;

    // ── Piezas que ruedan: guardan su RectTransform, posición original y último valor mostrado ──
    class ParteRodante
    {
        public Text texto;
        public RectTransform rect;
        public Vector2 posOriginal;
        public string ultimoValor = "";
    }

    readonly ParteRodante parteDiaNumero = new ParteRodante();
    readonly ParteRodante parteFechaNumero = new ParteRodante();
    readonly ParteRodante parteFechaMes = new ParteRodante();

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        SetAlpha(mostrarIntroDia1 ? 1f : 0f);

        PrepararParte(parteDiaNumero, textoDiaNumero);
        PrepararParte(parteFechaNumero, textoFechaNumero);
        PrepararParte(parteFechaMes, textoFechaMes);

        DesactivarTodosLosTextos();

        for (int i = 0; i < npcsPorDia.Length; i++)
            if (npcsPorDia[i] != null)
                npcsPorDia[i].SetActive(i == 0);

        if (fuenteTransicion == null)
        {
            fuenteTransicion = gameObject.AddComponent<AudioSource>();
            fuenteTransicion.playOnAwake = false;
            fuenteTransicion.loop = false;
            fuenteTransicion.spatialBlend = 0f;
        }
    }

    void PrepararParte(ParteRodante parte, Text texto)
    {
        if (texto == null) return;
        parte.texto = texto;
        parte.rect = texto.GetComponent<RectTransform>();
        parte.posOriginal = parte.rect.anchoredPosition;
    }

    void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnFinDia += OnFinDia;
        else
            Debug.LogError("[TransicionDia] GameManager.Instance es null en Start.");

        if (mostrarIntroDia1)
        {
            if (firstPersonController != null)
                firstPersonController.enabled = false;
            StartCoroutine(IntroDia1CO());
        }
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnFinDia -= OnFinDia;
    }

    // ─────────────────────────────────────────────────────────────────────
    void OnApplicationFocus(bool tieneFoco)
    {
        // Al volver a la ventana (Alt+Tab, Win, clic en barra de tareas…)
        // re-bloquea el cursor si el juego está en modo exploración
        // (es decir, no hay diálogo abierto ni menú de pausa).
        if (!tieneFoco) return;

        // Si el firstPersonController está activo = el jugador tiene control → bloquear cursor
        if (firstPersonController != null && firstPersonController.enabled)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Fade de entrada al Día 1
    // NOTA: No silenciamos el AudioManager aquí porque InicializadorAudio
    // todavía no arrancó la música — el fade de Día 1 ya ocurre "antes" de que
    // la música empiece. InicializadorAudio.Start() corre en el mismo frame que
    // TransicionDia.Start(), así que dejamos que se solapen naturalmente.
    // ─────────────────────────────────────────────────────────────────────
    IEnumerator IntroDia1CO()
    {
        // Cursor bloqueado e invisible durante toda la intro
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Bloquear texto "Presiona E" durante toda la intro del Día 1
        SistemaDialogo.BloquearInteraccion = true;

        // Sonido de transición al entrar al Día 1
        if (fuenteTransicion != null && clipTransicion != null)
            fuenteTransicion.PlayOneShot(clipTransicion, volumenTransicion);

        MostrarTextos(nombreDia1, fechaDia1);
        yield return new WaitForSeconds(duracionTexto);
        OcultarTextos();
        yield return Fade(1f, 0f, duracionFadeOut);

        // Arrancar música normal del juego al terminar el fade del Día 1
        if (AudioManager.Instance != null)
            AudioManager.Instance.IniciarMusicaJuego();

        if (firstPersonController != null)
            firstPersonController.enabled = true;

        // Desbloquear interacción al terminar la intro
        SistemaDialogo.BloquearInteraccion = false;

        // ── Aviso "Habla con SamuVR" — espera mientras dura el tutorial ──
        StartCoroutine(MostrarAvisoInicioDiaCO(0, delayAvisoDia1));
    }

    // ─────────────────────────────────────────────────────────────────────
    void OnFinDia(int diaQueTermino)
    {
        StartCoroutine(TransicionCO(diaQueTermino));
    }

    IEnumerator TransicionCO(int diaQueTermino)
    {
        int indiceSiguiente = diaQueTermino;

        // Cursor bloqueado e invisible durante toda la transición
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Bloquear texto "Presiona E" durante toda la transición de día
        SistemaDialogo.BloquearInteraccion = true;

        if (firstPersonController != null)
            firstPersonController.enabled = false;

        // Silenciar música ANTES del fade a negro
        if (AudioManager.Instance != null)
            AudioManager.Instance.SilenciarParaTransicion();

        yield return Fade(0f, 1f, duracionFadeIn);

        // Sonido de transición de día
        if (fuenteTransicion != null && clipTransicion != null)
            fuenteTransicion.PlayOneShot(clipTransicion, volumenTransicion);

        // Teletransportar jugador
        if (playerTransform != null && indiceSiguiente < spawnsPorDia.Length && spawnsPorDia[indiceSiguiente] != null)
        {
            CharacterController cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            playerTransform.position = spawnsPorDia[indiceSiguiente].position;
            playerTransform.rotation = spawnsPorDia[indiceSiguiente].rotation;
            if (cc != null) cc.enabled = true;
        }

        SwapNPC(indiceSiguiente);

        int indice = diaQueTermino - 1;
        string nombreDia = (indice >= 0 && indice < nombresDias.Length) ? nombresDias[indice] : $"Día {diaQueTermino + 1}";
        string fecha = (indice >= 0 && indice < fechas.Length) ? fechas[indice] : "";
        MostrarTextos(nombreDia, fecha);

        yield return new WaitForSeconds(duracionTexto);
        OcultarTextos();
        yield return Fade(1f, 0f, duracionFadeOut);

        // Restaurar música DESPUÉS del fade de salida
        if (AudioManager.Instance != null)
            AudioManager.Instance.RestaurarMusica();

        if (firstPersonController != null)
            firstPersonController.enabled = true;

        // Desbloquear interacción al terminar la transición
        SistemaDialogo.BloquearInteraccion = false;

        // ── Aviso "Habla con SamuVR" — solo días 2 y 3 (indiceSiguiente 1 y 2) ──
        // indiceSiguiente 3 = Día 4 → no muestra aviso
        if (indiceSiguiente < 3)
            StartCoroutine(MostrarAvisoInicioDiaCO(indiceSiguiente, delayAvisoTransicion));
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Espera 'delay' segundos y pide al SistemaDialogo de ese día
    /// que muestre el aviso persistente "Habla con SamuVR".
    /// indiceDia: 0 = Día 1, 1 = Día 2, 2 = Día 3 (el 3 no se llama).
    /// </summary>
    IEnumerator MostrarAvisoInicioDiaCO(int indiceDia, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (dialogoInicioPorDia != null &&
            indiceDia >= 0 && indiceDia < dialogoInicioPorDia.Length &&
            dialogoInicioPorDia[indiceDia] != null)
        {
            dialogoInicioPorDia[indiceDia].MostrarAvisoInicioDia();
        }
        else
        {
            Debug.LogWarning($"[TransicionDia] dialogoInicioPorDia[{indiceDia}] no está asignado. " +
                             "Asigna el SistemaDialogo del momento de inicio de ese día en el Inspector.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Recibe el texto completo de siempre (ej: "Día 2", "4 de abril, 2026")
    /// y lo separa en partes: solo el número del día y solo el número+mes de
    /// la fecha ruedan (efecto odómetro); "Día", "de" y ", año" quedan fijos.
    /// No hace falta cambiar cómo cargás nombresDias[] / fechas[] — se parsean solos.
    /// </summary>
    void MostrarTextos(string nombreDiaCompleto, string fechaCompleta)
    {
        var (prefijoDia, numeroDia) = ParsearDia(nombreDiaCompleto);
        var (numeroFecha, mesFecha, sufijoFecha) = ParsearFecha(fechaCompleta);

        // Partes estáticas: se muestran directo, sin animación
        if (textoDiaEtiqueta != null) { textoDiaEtiqueta.text = prefijoDia; textoDiaEtiqueta.enabled = true; }
        if (textoFechaConector != null) { textoFechaConector.text = "de"; textoFechaConector.enabled = true; }
        if (textoFechaSufijo != null) { textoFechaSufijo.text = ", " + sufijoFecha; textoFechaSufijo.enabled = true; }

        // Partes que ruedan
        StartCoroutine(RodarTextoCO(parteDiaNumero, numeroDia));
        StartCoroutine(RodarTextoCO(parteFechaNumero, numeroFecha));
        StartCoroutine(RodarTextoCO(parteFechaMes, mesFecha));
    }

    /// <summary>Separa "Día 2" en prefijo="Día" y numero="2".</summary>
    (string prefijo, string numero) ParsearDia(string texto)
    {
        Match m = Regex.Match(texto, @"^(.*?)(\d+)\s*$");
        if (m.Success) return (m.Groups[1].Value.Trim(), m.Groups[2].Value);
        return ("", texto);
    }

    /// <summary>Separa "4 de abril, 2026" en numero="4", mes="abril", sufijo="2026".</summary>
    (string numero, string mes, string sufijo) ParsearFecha(string texto)
    {
        Match m = Regex.Match(texto, @"^(\d+)\s+de\s+([^,]+),\s*(.+)$");
        if (m.Success) return (m.Groups[1].Value, m.Groups[2].Value.Trim(), m.Groups[3].Value.Trim());
        return (texto, "", "");
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Efecto "odómetro" para UNA pieza de texto (un número o el mes):
    /// el valor anterior se clona y rueda hacia abajo desapareciendo,
    /// mientras el valor nuevo entra rodando desde arriba hasta su lugar.
    /// Si no había valor anterior (primera vez), solo entra el nuevo.
    /// </summary>
    IEnumerator RodarTextoCO(ParteRodante parte, string nuevoValor)
    {
        if (parte.texto == null || parte.rect == null) yield break;

        Text clonSaliente = null;
        if (!string.IsNullOrEmpty(parte.ultimoValor))
        {
            clonSaliente = Instantiate(parte.texto, parte.texto.transform.parent);
            clonSaliente.text = parte.ultimoValor;
            clonSaliente.enabled = true;
            clonSaliente.rectTransform.anchoredPosition = parte.posOriginal;
            SetAlphaTexto(clonSaliente, 1f);
        }

        Vector2 entrada = parte.posOriginal + Vector2.up * distanciaDeslizamiento;
        Vector2 salida = parte.posOriginal - Vector2.up * distanciaDeslizamiento;

        parte.texto.text = nuevoValor;
        parte.texto.enabled = true;
        parte.rect.anchoredPosition = entrada;
        SetAlphaTexto(parte.texto, 0f);

        float t = 0f;
        while (t < duracionAnimacionTexto)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duracionAnimacionTexto);
            float suavizado = 1f - Mathf.Pow(1f - p, 3f); // ease-out cúbico

            parte.rect.anchoredPosition = Vector2.Lerp(entrada, parte.posOriginal, suavizado);
            SetAlphaTexto(parte.texto, suavizado);

            if (clonSaliente != null)
            {
                clonSaliente.rectTransform.anchoredPosition = Vector2.Lerp(parte.posOriginal, salida, suavizado);
                SetAlphaTexto(clonSaliente, 1f - suavizado);
            }

            yield return null;
        }

        parte.rect.anchoredPosition = parte.posOriginal;
        SetAlphaTexto(parte.texto, 1f);
        if (clonSaliente != null) Destroy(clonSaliente.gameObject);

        parte.ultimoValor = nuevoValor;
    }

    void SetAlphaTexto(Text texto, float a)
    {
        if (texto == null) return;
        Color c = texto.color;
        c.a = a;
        texto.color = c;
    }

    void OcultarTextos()
    {
        DesactivarTodosLosTextos();
    }

    void DesactivarTodosLosTextos()
    {
        if (textoDiaEtiqueta != null) textoDiaEtiqueta.enabled = false;
        if (textoDiaNumero != null) textoDiaNumero.enabled = false;
        if (textoFechaNumero != null) textoFechaNumero.enabled = false;
        if (textoFechaConector != null) textoFechaConector.enabled = false;
        if (textoFechaMes != null) textoFechaMes.enabled = false;
        if (textoFechaSufijo != null) textoFechaSufijo.enabled = false;
    }

    void SwapNPC(int indiceActivo)
    {
        for (int i = 0; i < npcsPorDia.Length; i++)
            if (npcsPorDia[i] != null)
                npcsPorDia[i].SetActive(i == indiceActivo);
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator Fade(float desde, float hasta, float duracion)
    {
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Lerp(desde, hasta, t / duracion));
            yield return null;
        }
        SetAlpha(hasta);
    }

    void SetAlpha(float a)
    {
        if (panelNegro == null) return;
        Color c = panelNegro.color;
        c.a = a;
        panelNegro.color = c;
    }
}
