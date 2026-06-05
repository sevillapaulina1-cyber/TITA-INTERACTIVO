using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Maneja la transición visual entre días:
///   fundido a negro → teletransporta jugador y NPC → muestra fecha → fundido de regreso.
///
/// SETUP EN INSPECTOR:
///   - panelNegro        → Image negra en stretch completo (Canvas Sort Order 99)
///   - textoDia          → Text centrado para mostrar la fecha
///   - npcsPorDia        → 4 NPCs duplicados en orden (índice 0 = Día 1)
///   - spawnsPorDia      → 4 GameObjects vacíos con las posiciones de spawn del jugador
///   - playerTransform   → Transform del jugador (para teletransportarlo)
///   - fechas            → 3 strings: fecha del día 2, 3 y 4
/// </summary>
public class TransicionDia : MonoBehaviour
{
    [Header("── UI ──────────────────────────────────")]
    public Image panelNegro;
    public Text textoDia;         // muestra la fecha    ej: "4 de abril, 2026"
    public Text textoDiaNumero;   // muestra el día      ej: "Día 2"

    [Header("── NPCs (uno por día, en orden) ─────────")]
    [Tooltip("4 NPCs duplicados. Índice 0 = Día 1, 1 = Día 2, etc.")]
    public GameObject[] npcsPorDia;

    [Header("── Spawns del jugador (uno por día) ──────")]
    [Tooltip("4 GameObjects vacíos con la posición de inicio de cada día")]
    public Transform[] spawnsPorDia;

    [Header("── Referencia al jugador ───────────────")]
    public Transform playerTransform;
    public MonoBehaviour firstPersonController;

    [Header("── Fechas de transición ───────────────")]
    [Tooltip("3 entradas: fecha que aparece al pasar al día 2, 3 y 4")]
    public string[] fechas = {
        "4 de abril, 2026",
        "2 de mayo, 2026",
        "20 de mayo, 2026"
    };

    [Tooltip("3 entradas: nombre del día al pasar al día 2, 3 y 4")]
    public string[] nombresDias = {
        "Día 2",
        "Día 3",
        "Día 4"
    };

    [Header("── Tiempos ─────────────────────────────")]
    public float duracionFadeIn = 1.0f;
    public float duracionTexto = 2.0f;
    public float duracionFadeOut = 1.0f;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        SetAlpha(0f);
        if (textoDia != null) textoDia.enabled = false;
        if (textoDiaNumero != null) textoDiaNumero.enabled = false;

        // Solo el NPC del día 1 activo al inicio
        for (int i = 0; i < npcsPorDia.Length; i++)
            if (npcsPorDia[i] != null)
                npcsPorDia[i].SetActive(i == 0);
    }

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFinDia += OnFinDia;
            Debug.Log("[TransicionDia] Suscrito a OnFinDia correctamente.");
        }
        else
        {
            Debug.LogError("[TransicionDia] GameManager.Instance es null en Start. Verifica el orden de ejecución.");
        }
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnFinDia -= OnFinDia;
    }

    // ─────────────────────────────────────────────────────────────────────
    void OnFinDia(int diaQueTermino)
    {
        StartCoroutine(TransicionCO(diaQueTermino));
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator TransicionCO(int diaQueTermino)
    {
        int indiceSiguiente = diaQueTermino; // día 1 termina → activa índice 1
        Debug.Log($"[TransicionDia] Iniciando transición. diaQueTermino={diaQueTermino} indiceSiguiente={indiceSiguiente}");

        // Bloquear jugador
        if (firstPersonController != null)
            firstPersonController.enabled = false;

        // 1. Fade a negro
        Debug.Log("[TransicionDia] Iniciando fade a negro...");
        yield return Fade(0f, 1f, duracionFadeIn);
        Debug.Log("[TransicionDia] Fade completado.");

        // 2. Teletransportar jugador
        if (playerTransform == null)
            Debug.LogError("[TransicionDia] playerTransform es NULL.");
        else if (indiceSiguiente >= spawnsPorDia.Length)
            Debug.LogError($"[TransicionDia] indiceSiguiente={indiceSiguiente} fuera de rango. spawnsPorDia.Length={spawnsPorDia.Length}");
        else if (spawnsPorDia[indiceSiguiente] == null)
            Debug.LogError($"[TransicionDia] spawnsPorDia[{indiceSiguiente}] es NULL.");
        else
        {
            // Desactivar CharacterController temporalmente si existe
            CharacterController cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            playerTransform.position = spawnsPorDia[indiceSiguiente].position;
            playerTransform.rotation = spawnsPorDia[indiceSiguiente].rotation;

            if (cc != null) cc.enabled = true;
            Debug.Log($"[TransicionDia] Jugador teletransportado a {spawnsPorDia[indiceSiguiente].name}");
        }

        // 3. Swap de NPC
        SwapNPC(indiceSiguiente);
        Debug.Log($"[TransicionDia] NPC swapeado a índice {indiceSiguiente}");

        // 4. Mostrar día y fecha
        if (textoDia != null || textoDiaNumero != null)
        {
            int indice = diaQueTermino - 1;

            if (textoDiaNumero != null)
            {
                textoDiaNumero.text = (indice >= 0 && indice < nombresDias.Length)
                    ? nombresDias[indice]
                    : $"Día {diaQueTermino + 1}";
                textoDiaNumero.enabled = true;
            }

            if (textoDia != null)
            {
                textoDia.text = (indice >= 0 && indice < fechas.Length)
                    ? fechas[indice]
                    : "";
                textoDia.enabled = true;
            }
        }

        yield return new WaitForSeconds(duracionTexto);

        // 5. Ocultar textos
        if (textoDia != null) textoDia.enabled = false;
        if (textoDiaNumero != null) textoDiaNumero.enabled = false;

        // 6. Fade de regreso
        yield return Fade(1f, 0f, duracionFadeOut);

        // 7. Devolver control al jugador
        if (firstPersonController != null)
            firstPersonController.enabled = true;
    }

    // ─────────────────────────────────────────────────────────────────────
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

