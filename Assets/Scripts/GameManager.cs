using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum TipoEleccion
{
    Verde,
    Neutro,
    Rojo
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int PuntosConfianza { get; private set; } = 0;
    public int PuntosRiesgo { get; private set; } = 0;
    public int PuntosNeutros { get; private set; } = 0;

    public int MomentoActual { get; private set; } = 0;
    public int DiaActual { get; private set; } = 1;
    public int DecisionesEnEsteDia { get; private set; } = 0;

    public const int TOTAL_MOMENTOS = 12;
    public const int DECISIONES_POR_DIA = 3;
    public const int TOTAL_DIAS = 4;

    [Header("Escenas de final")]
    public string escenaFinal1 = "Final_1";
    public string escenaFinal2 = "Final_2";

    public int EleccionesVerdes { get; private set; } = 0;
    public int EleccionesRojas { get; private set; } = 0;
    public int EleccionesNeutras { get; private set; } = 0;

    public TipoEleccion[] HistorialElecciones { get; private set; } = new TipoEleccion[TOTAL_MOMENTOS];

    // ── ▼ NUEVO: guarda el texto exacto que el jugador eligió en cada momento ──
    public string[] HistorialTextos { get; private set; } = new string[TOTAL_MOMENTOS];
    // ── ▲ NUEVO ─────────────────────────────────────────────────────────────

    public event System.Action<int> OnFinDia;
    public event System.Action OnJuegoCompleto;

    [Header("── Debug (quitar antes del build) ──────")]
    [Tooltip("Si > 0, inicia desde ese momento. Ej: 4 para probar el puzzle de palancas.")]
    public int debugIniciarDesdeMomento = 0;
    [Tooltip("Día correspondiente al momento de debug")]
    public int debugDiaInicio = 1;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (debugIniciarDesdeMomento > 0)
        {
            MomentoActual = debugIniciarDesdeMomento;
            DiaActual = debugDiaInicio;
            DecisionesEnEsteDia = debugIniciarDesdeMomento % DECISIONES_POR_DIA;
            Debug.Log($"[GM] DEBUG: iniciando desde momento {debugIniciarDesdeMomento + 1}, día {debugDiaInicio}");
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    public void RegistrarEleccion(TipoEleccion eleccion, string textoEleccion = "")
    {
        MomentoActual++;
        DecisionesEnEsteDia++;

        HistorialElecciones[MomentoActual - 1] = eleccion;
        HistorialTextos[MomentoActual - 1] = textoEleccion;

        switch (eleccion)
        {
            case TipoEleccion.Verde: PuntosConfianza += 2; EleccionesVerdes++; break;
            case TipoEleccion.Neutro: PuntosConfianza += 1; PuntosNeutros += 1; EleccionesNeutras++; break;
            case TipoEleccion.Rojo: PuntosRiesgo += 2; EleccionesRojas++; break;
        }

        Debug.Log($"[GM] Día {DiaActual} | Decisión {DecisionesEnEsteDia}/{DECISIONES_POR_DIA} | Momento {MomentoActual}/{TOTAL_MOMENTOS} | Confianza:{PuntosConfianza} Riesgo:{PuntosRiesgo}");

        if (DecisionesEnEsteDia >= DECISIONES_POR_DIA)
        {
            if (MomentoActual >= TOTAL_MOMENTOS)
            {
                OnJuegoCompleto?.Invoke();
                EvaluarFinal();
            }
            else
            {
                int diaQueTermina = DiaActual;
                DiaActual++;
                DecisionesEnEsteDia = 0;
                OnFinDia?.Invoke(diaQueTermina);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    void EvaluarFinal()
    {
        string escena = (PuntosConfianza >= PuntosRiesgo) ? escenaFinal1 : escenaFinal2;
        Debug.Log($"[GM] FINAL → {escena}");
        StartCoroutine(FadeYCargarCO(escena));
    }

    IEnumerator FadeYCargarCO(string escena)
    {
        TransicionDia transicion = FindAnyObjectByType<TransicionDia>();
        if (transicion != null && transicion.panelNegro != null)
        {
            float t = 0f;
            while (t < 1.5f)
            {
                t += Time.deltaTime;
                Color c = transicion.panelNegro.color;
                c.a = Mathf.Lerp(0f, 1f, t / 1.5f);
                transicion.panelNegro.color = c;
                yield return null;
            }
        }
        else yield return new WaitForSeconds(0.5f);

        SceneManager.LoadScene(escena);
    }

    // ─────────────────────────────────────────────────────────────────────
    // ── ▼ NUEVO: única fuente de verdad para el desenlace, usada tanto por
    //     UIRetroalimentacion como por MapaDecisiones ──────────────────────
    public bool EsFinal1 => PuntosConfianza >= PuntosRiesgo;

    public string ObtenerTituloFinal()
    {
        return EsFinal1 ? "Final 1 — Secuestro" : "Final 2 — Policía";
    }

    public string ObtenerMensajeFinal()
    {
        return EsFinal1
            ? "El niño confió mucho en ti y lograste que no avisara a nadie."
            : "Tu lenguaje fue muy sospechoso, el niño avisó a sus padres.";
    }
    // ── ▲ NUEVO ─────────────────────────────────────────────────────────────

    // ─────────────────────────────────────────────────────────────────────
    public string ObtenerResumen()
    {
        string finalStr = ObtenerTituloFinal();
        return $"=== Resumen de tu recorrido ===\n\n" +
               $"Decisiones protectoras (verde):  {EleccionesVerdes}  → +{EleccionesVerdes * 2} pts\n" +
               $"Decisiones ambiguas   (neutro):  {EleccionesNeutras} → +{EleccionesNeutras} pts\n" +
               $"Decisiones vulnerables (rojo):   {EleccionesRojas}  → +{EleccionesRojas * 2} pts\n\n" +
               $"Total Confianza: {PuntosConfianza}\nTotal Riesgo: {PuntosRiesgo}\n\nDesenlace: {finalStr}\n\n{ObtenerMensajeFinal()}";
    }

    // ─────────────────────────────────────────────────────────────────────
    public void Reiniciar()
    {
        PuntosConfianza = PuntosRiesgo = PuntosNeutros = 0;
        MomentoActual = 0; DiaActual = 1; DecisionesEnEsteDia = 0;
        EleccionesVerdes = EleccionesRojas = EleccionesNeutras = 0;
        HistorialElecciones = new TipoEleccion[TOTAL_MOMENTOS];
        HistorialTextos = new string[TOTAL_MOMENTOS];
        if (Camera.main != null) Camera.main.fieldOfView = 60f;
    }
}