using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton persistente que gestiona:
///   - Puntos de Confianza y Riesgo
///   - Los 12 momentos de decisión repartidos en 4 días (3 decisiones por día)
///   - La evaluación del final al completar el día 4
///
/// NO necesita modificarse entre momentos. Todo se configura desde
/// SistemaDialogo.cs en el Inspector.
/// </summary>

public enum TipoEleccion
{
    Verde,   // +2 Confianza — decisión protectora
    Neutro,  // +1 Confianza — decisión ambigua
    Rojo     // +2 Riesgo    — decisión vulnerable
}

public class GameManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    // ── Puntos ────────────────────────────────────────────────────────────
    public int PuntosConfianza { get; private set; } = 0;
    public int PuntosRiesgo { get; private set; } = 0;
    public int PuntosNeutros { get; private set; } = 0;

    // ── Contadores ────────────────────────────────────────────────────────
    public int MomentoActual { get; private set; } = 0;  // 0–12
    public int DiaActual { get; private set; } = 1;  // 1–4
    public int DecisionesEnEsteDia { get; private set; } = 0;  // 0–3

    public const int TOTAL_MOMENTOS = 12;
    public const int DECISIONES_POR_DIA = 3;
    public const int TOTAL_DIAS = 4;

    // ── Escenas de final ──────────────────────────────────────────────────
    [Header("Escenas de final (ajusta los nombres en el Inspector)")]
    public string escenaFinal1 = "Final1_Secuestro";
    public string escenaFinal2 = "Final2_Policia";

    // ── Retroalimentación ─────────────────────────────────────────────────
    public int EleccionesVerdes { get; private set; } = 0;
    public int EleccionesRojas { get; private set; } = 0;
    public int EleccionesNeutras { get; private set; } = 0;

    // ── Evento: fin de día (TransicionDia se suscribe aquí) ───────────────
    public event System.Action<int> OnFinDia;   // parámetro = día que acaba de terminar
    public event System.Action OnJuegoCompleto;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Llamar desde SistemaDialogo al confirmar una elección.
    /// </summary>
    public void RegistrarEleccion(TipoEleccion eleccion)
    {
        MomentoActual++;
        DecisionesEnEsteDia++;

        switch (eleccion)
        {
            case TipoEleccion.Verde:
                PuntosConfianza += 2;
                EleccionesVerdes++;
                break;
            case TipoEleccion.Neutro:
                PuntosConfianza += 1;
                PuntosNeutros += 1;
                EleccionesNeutras++;
                break;
            case TipoEleccion.Rojo:
                PuntosRiesgo += 2;
                EleccionesRojas++;
                break;
        }

        Debug.Log($"[GM] Día {DiaActual} | Decisión {DecisionesEnEsteDia}/{DECISIONES_POR_DIA} " +
                  $"| Momento {MomentoActual}/{TOTAL_MOMENTOS} " +
                  $"| Confianza:{PuntosConfianza} Riesgo:{PuntosRiesgo}");

        // ¿Terminó el día?
        if (DecisionesEnEsteDia >= DECISIONES_POR_DIA)
        {
            if (MomentoActual >= TOTAL_MOMENTOS)
            {
                // Último día completado → ir al final
                Debug.Log("[GM] Juego completo, cargando final...");
                OnJuegoCompleto?.Invoke();
                EvaluarFinal();
            }
            else
            {
                // Fin de día normal → avisar a TransicionDia
                int diaQueTermina = DiaActual;
                DiaActual++;
                DecisionesEnEsteDia = 0;
                Debug.Log($"[GM] Fin día {diaQueTermina} → disparando OnFinDia. Suscriptores: {OnFinDia?.GetInvocationList().Length ?? 0}");
                OnFinDia?.Invoke(diaQueTermina);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    void EvaluarFinal()
    {
        string escena = (PuntosConfianza >= PuntosRiesgo) ? escenaFinal1 : escenaFinal2;
        Debug.Log($"[GM] FINAL → {escena} | Confianza:{PuntosConfianza} Riesgo:{PuntosRiesgo}");
        StartCoroutine(FadeYCargarCO(escena));
    }

    IEnumerator FadeYCargarCO(string escena)
    {
        // Buscar el panel negro del TransicionDia para reutilizarlo
        TransicionDia transicion = FindAnyObjectByType<TransicionDia>();

        if (transicion != null && transicion.panelNegro != null)
        {
            // Fade a negro usando el mismo panel de transición
            float t = 0f;
            float duracion = 1.5f;
            UnityEngine.UI.Image panel = transicion.panelNegro;

            while (t < duracion)
            {
                t += Time.deltaTime;
                Color c = panel.color;
                c.a = Mathf.Lerp(0f, 1f, t / duracion);
                panel.color = c;
                yield return null;
            }
        }
        else
        {
            // Si no hay panel, esperar un momento antes de cargar
            yield return new WaitForSeconds(0.5f);
        }

        SceneManager.LoadScene(escena);
    }

    // ─────────────────────────────────────────────────────────────────────
    public string ObtenerResumen()
    {
        string finalStr = (PuntosConfianza >= PuntosRiesgo)
            ? "Final 1 — Secuestro"
            : "Final 2 — Policía";

        return $"=== Resumen de tu recorrido ===\n\n" +
               $"Decisiones protectoras (verde):  {EleccionesVerdes}  → +{EleccionesVerdes * 2} pts Confianza\n" +
               $"Decisiones ambiguas   (neutro):  {EleccionesNeutras} → +{EleccionesNeutras} pts Confianza\n" +
               $"Decisiones vulnerables (rojo):   {EleccionesRojas}  → +{EleccionesRojas * 2} pts Riesgo\n\n" +
               $"Total Confianza: {PuntosConfianza}\n" +
               $"Total Riesgo:    {PuntosRiesgo}\n\n" +
               $"Desenlace: {finalStr}";
    }

    // ─────────────────────────────────────────────────────────────────────
    public void Reiniciar()
    {
        PuntosConfianza = 0;
        PuntosRiesgo = 0;
        PuntosNeutros = 0;
        MomentoActual = 0;
        DiaActual = 1;
        DecisionesEnEsteDia = 0;
        EleccionesVerdes = 0;
        EleccionesRojas = 0;
        EleccionesNeutras = 0;

        // Restaurar FOV de la cámara principal al reiniciar
        if (Camera.main != null)
            Camera.main.fieldOfView = 60f;
    }
}