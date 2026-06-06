using UnityEngine;

public class RocketPhysics : MonoBehaviour
{
    // 1 unidade Unity = ESCALA metros
    public const float ESCALA = 100f;

    [Header("Arrasto")]
    public float coeficienteArrasto = 0.75f;
    public float areaFrontal        = 10.52f; // m²

    [Header("Massa estrutural (kg)")]
    public float massaEstrutural = 22200f; // Falcon 9 first stage estrutural ≈ 22t

    [Header("Estágio")]
    public float massaEstagio    = 5000f;  // kg removidos ao separar
    [HideInInspector] public bool estagioSeparado = false;

    [HideInInspector] public Vector2 posicao;
    [HideInInspector] public Vector2 velocidade;
    [HideInInspector] public PlanetaController[] planetas;

    public float MassaTotal(float massaCombustivel) => massaEstrutural + massaCombustivel;

    /// <summary>
    /// Retorna aceleração em unidades Unity/s² (dividido por ESCALA no final).
    /// pos      : posição em unidades Unity
    /// velMS    : velocidade em m/s  (= velocidade * ESCALA)
    /// massaAtual: kg
    /// </summary>
    public Vector2 CalcularGravArrasto(Vector2 pos, Vector2 velMS, float massaAtual)
    {
        Vector2 accMetros = Vector2.zero;

        foreach (PlanetaController p in planetas)
        {
            // Vetor direção: ambos em unidades Unity → normalização correta
            Vector2 diff = (Vector2)p.transform.position - pos;
            float distUnity = diff.magnitude;
            if (distUnity < 0.001f) continue;

            Vector2 dir = diff / distUnity;

            // Distância em metros
            float distMetros = distUnity * ESCALA;

            // ── GRAVIDADE ──────────────────────────────────────────────────
            // g = GM / dist²   (m/s²)
            // Usamos p.GM que já é gravidadeSuperficie * raio²
            float grav = p.GM / (distMetros * distMetros);
            accMetros += dir * grav;

            // ── ARRASTO ATMOSFÉRICO ────────────────────────────────────────
            float altitude = distMetros - p.raio;

            // Sem atmosfera acima de 10x a altura de escala ou abaixo da superfície
            if (altitude < 0f || altitude > p.alturaEscalaAtmosfera * 10f) continue;

            float v = velMS.magnitude;
            if (v < 0.01f) continue;

            float densidade = p.densidadeArNivelMar * Mathf.Exp(-altitude / p.alturaEscalaAtmosfera);
            float forceArr  = 0.5f * densidade * v * v * coeficienteArrasto * areaFrontal; // N
            float accArr    = forceArr / massaAtual; // m/s²

            accMetros -= velMS.normalized * accArr;
        }

        // Converte m/s² → unidades Unity/s²
        return accMetros / ESCALA;
    }

    /// <summary>
    /// Integração Euler semi-implícita.
    /// vel, pos    : unidades Unity  e  unidades Unity
    /// accTotal    : unidades Unity/s²
    /// </summary>
    public (Vector2 novaVel, Vector2 novaPosicao) Integrar(
        Vector2 vel, Vector2 pos, Vector2 accTotal, float dt)
    {
        Vector2 novaVel      = vel + accTotal * dt;
        Vector2 novaPosicao  = pos + novaVel  * dt;
        return (novaVel, novaPosicao);
    }

    /// <summary>Altitude em metros acima do planeta mais próximo.</summary>
    public float AltitudePlanetaMaisProximo(Vector2 pos)
    {
        float menorAlt = float.MaxValue;
        foreach (PlanetaController p in planetas)
        {
            float distM = Vector2.Distance(pos, (Vector2)p.transform.position) * ESCALA;
            float alt   = distM - p.raio;
            if (alt < menorAlt) menorAlt = alt;
        }
        return menorAlt;
    }

    public void SepararEstagio()
    {
        if (estagioSeparado) return;
        massaEstrutural = Mathf.Max(massaEstrutural - massaEstagio, 0f);
        estagioSeparado = true;
        Debug.Log($"Estágio separado! Massa estrutural agora: {massaEstrutural} kg");
    }
}