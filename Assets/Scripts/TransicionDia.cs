using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TransicionDia : MonoBehaviour
{
    [Header("── UI ──────────────────────────────────")]
    public Image panelNegro;
    public Text textoDia;
    public Text textoDiaNumero;

    [Header("── NPCs (uno por día, en orden) ─────────")]
    [Tooltip("4 NPCs duplicados. Índice 0 = Día 1, 1 = Día 2, etc.")]
    public GameObject[] npcsPorDia;

    [Header("── Aviso \"Habla con SamuVR\" por día ─────")]
    [Tooltip("SistemaDialogo del momento de inicio de cada día (1, 4, 7, 10). Índice 0 = Día 1, 1 = Día 2, etc. El de Día 4 (índice 3) puede dejarse vacío ya que no debe mostrar el aviso.")]
    public SistemaDialogo[] dialogoInicioPorDia;
    [Tooltip("Segundos extra de espera tras el fade de día antes de mostrar el aviso (para que coincida con el tiempo visible del tutorial, ~20s en Día 1)")]
    public float delayAvisoDia1 = 20f;
    [Tooltip("Segundos extra de espera tras el fade de transición (días 2 y 3) antes de mostrar el aviso")]
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

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        SetAlpha(mostrarIntroDia1 ? 1f : 0f);
        if (textoDia != null) textoDia.enabled = false;
        if (textoDiaNumero != null) textoDiaNumero.enabled = false;

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
    // Fade de entrada al Día 1
    // NOTA: No silenciamos el AudioManager aquí porque InicializadorAudio
    // todavía no arrancó la música — el fade de Día 1 ya ocurre "antes" de que
    // la música empiece. InicializadorAudio.Start() corre en el mismo frame que
    // TransicionDia.Start(), así que dejamos que se solapen naturalmente.
    // ─────────────────────────────────────────────────────────────────────
    IEnumerator IntroDia1CO()
    {
        MostrarTextos(nombreDia1, fechaDia1);
        yield return new WaitForSeconds(duracionTexto);
        OcultarTextos();
        yield return Fade(1f, 0f, duracionFadeOut);

        if (firstPersonController != null)
            firstPersonController.enabled = true;

        // ── Aviso "Habla con SamuVR" — espera lo que dura el tutorial ──────
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

        // ── Aviso "Habla con SamuVR" — solo si NO es el Día 4 ──────────────
        // indiceSiguiente: 1 = Día 2, 2 = Día 3, 3 = Día 4 (no se muestra)
        if (indiceSiguiente < 3)
            StartCoroutine(MostrarAvisoInicioDiaCO(indiceSiguiente, delayAvisoTransicion));
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Espera 'delay' segundos y luego pide al SistemaDialogo correspondiente
    /// a ese día que muestre el aviso persistente "Habla con SamuVR".
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
    }

    // ─────────────────────────────────────────────────────────────────────
    void MostrarTextos(string nombreDia, string fecha)
    {
        if (textoDiaNumero != null) { textoDiaNumero.text = nombreDia; textoDiaNumero.enabled = true; }
        if (textoDia != null) { textoDia.text = fecha; textoDia.enabled = true; }
    }

    void OcultarTextos()
    {
        if (textoDia != null) textoDia.enabled = false;
        if (textoDiaNumero != null) textoDiaNumero.enabled = false;
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

