using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RocketTrajectory : MonoBehaviour
{
    public const float ESCALA = 100f;

    [Header("Trajetória")]
    public int   passos           = 150;
    public float tempoEntrePontos = 0.5f;

    [Header("Visual")]
    public Color corInicio = new Color(1f, 1f, 1f, 0.8f);
    public Color corFim    = new Color(1f, 1f, 1f, 0.1f);
    public float largura   = 0.05f;

    private LineRenderer line;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.material   = new Material(Shader.Find("Sprites/Default"));
        line.startColor = corInicio;
        line.endColor   = corFim;
        line.startWidth = largura;
        line.endWidth   = largura * 0.3f;
        line.useWorldSpace = true;
    }

    public void Desenhar(
        Vector2 posicaoAtual,    Vector2 velocidadeAtual,
        float massaCombustivel,  float massaEstrutural,
        float empuxoAtual,       float rotacaoFisica,     float anguloGimbal,
        bool lancando,           float throttlePercentual,
        float potenciaMaxima,    float consumoPorSegundo,  float tempoRespostaMotor,
        float coefArrasto,       float areaFrontal,
        PlanetaController[] planetas)
    {
        if (planetas == null || planetas.Length == 0) { Limpar(); return; }

        Vector2 pos       = posicaoAtual;
        Vector2 vel       = velocidadeAtual;
        float combustSim  = massaCombustivel;
        float empuxoSim   = empuxoAtual;
        float throttle    = throttlePercentual / 100f;

        line.positionCount = passos;
        int pontosValidos  = 0;
        float dt           = tempoEntrePontos;

        for (int i = 0; i < passos; i++)
        {
            float massaSim = massaEstrutural + combustSim;
            Vector2 velMS  = vel * ESCALA;
            Vector2 acc    = Vector2.zero; // em m/s²

            // ── GRAVIDADE + ARRASTO ────────────────────────────────────────
            foreach (PlanetaController p in planetas)
            {
                Vector2 diff      = (Vector2)p.transform.position - pos;
                float distUnity   = diff.magnitude;
                if (distUnity < 0.001f) continue;

                Vector2 dir       = diff / distUnity;
                float distMetros  = distUnity * ESCALA;

                // Gravidade usando GM
                acc += dir * (p.GM / (distMetros * distMetros));

                // Arrasto — só dentro da atmosfera e acima da superfície
                float altitude = distMetros - p.raio;
                if (altitude < 0f || altitude > p.alturaEscalaAtmosfera * 10f) continue;

                float v = velMS.magnitude;
                if (v < 0.01f) continue;

                float dens   = p.densidadeArNivelMar * Mathf.Exp(-altitude / p.alturaEscalaAtmosfera);
                float fArr   = 0.5f * dens * v * v * coefArrasto * areaFrontal;
                acc -= velMS.normalized * (fArr / massaSim);
            }

            // ── EMPUXO ─────────────────────────────────────────────────────
            if (combustSim > 0f && lancando)
            {
                float empuxoAlvo = potenciaMaxima * throttle;
                empuxoSim = Mathf.Lerp(empuxoSim, empuxoAlvo, dt / tempoRespostaMotor);

                float anguloTotal = rotacaoFisica + anguloGimbal;
                Vector2 dirSim    = new Vector2(
                    Mathf.Sin(-anguloTotal * Mathf.Deg2Rad),
                    Mathf.Cos( anguloTotal * Mathf.Deg2Rad));

                acc += dirSim * (empuxoSim / massaSim); // m/s²

                combustSim = Mathf.Max(combustSim - consumoPorSegundo * throttle * dt, 0f);
            }
            else
            {
                empuxoSim = Mathf.Lerp(empuxoSim, 0f, dt / tempoRespostaMotor);
            }

            // ── INTEGRAÇÃO ─────────────────────────────────────────────────
            // acc em m/s² → divide por ESCALA → unidades Unity/s²
            vel += (acc / ESCALA) * dt;
            pos += vel * dt;

            // ── VERIFICA COLISÃO COM SUPERFÍCIE ────────────────────────────
            bool bateu = false;
            foreach (PlanetaController p in planetas)
            {
                if (Vector2.Distance(pos, (Vector2)p.transform.position) <= p.raio / ESCALA)
                { bateu = true; break; }
            }
            if (bateu) break;

            line.SetPosition(pontosValidos, pos);
            pontosValidos++;
        }

        line.positionCount = pontosValidos;
    }

    public void Limpar() => line.positionCount = 0;
}