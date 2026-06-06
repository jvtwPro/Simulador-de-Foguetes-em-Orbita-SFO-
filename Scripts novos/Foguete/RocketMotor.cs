using UnityEngine;

public class RocketMotor : MonoBehaviour
{
    public const float ESCALA = 100f;

    [Header("Motor")]
    [Tooltip("Empuxo máximo em Newtons. Falcon 9: ~7.607.000 N")]
    public float potenciaMaxima = 7607000f;

    [Range(0f, 100f)]
    public float throttlePercentual = 100f;

    [Tooltip("Tempo para o motor atingir throttle alvo (segundos)")]
    public float tempoRespostaMotor = 1.5f;

    [Header("Combustível")]
    [Tooltip("Massa inicial de combustível em kg. Falcon 9 1st stage ≈ 411 000 kg")]
    public float massaCombustivelInicial = 123000f;

    [Tooltip("Consumo de combustível em kg/s no throttle máximo")]
    public float consumoPorSegundo = 2800f;

    [HideInInspector] public float empuxoAtual     = 0f;
    [HideInInspector] public float massaCombustivel;

    public bool  TemCombustivel => massaCombustivel > 0f;
    public float Throttle       => throttlePercentual / 100f;

    void Awake()
    {
        massaCombustivel = massaCombustivelInicial;
    }

    /// <summary>
    /// Calcula e retorna vetor de empuxo em Newtons.
    /// Desconta combustível consumido.
    /// </summary>
    public Vector2 CalcularEmpuxo(float dt, float rotacaoFisica, float anguloGimbal)
    {
        float throttle   = Throttle;
        float empuxoAlvo = potenciaMaxima * throttle;

        // Interpolação suave até o throttle alvo
        empuxoAtual = Mathf.Lerp(empuxoAtual, empuxoAlvo, dt / tempoRespostaMotor);

        // Direção do empuxo: para cima do foguete + deflexão do gimbal
        float anguloTotal = rotacaoFisica + anguloGimbal;
        Vector2 direcao   = new Vector2(
            Mathf.Sin(-anguloTotal * Mathf.Deg2Rad),
            Mathf.Cos( anguloTotal * Mathf.Deg2Rad));

        // Consome combustível
        float massaConsumida = consumoPorSegundo * throttle * dt;
        massaCombustivel     = Mathf.Max(massaCombustivel - massaConsumida, 0f);

        return direcao * empuxoAtual;
    }

    public void DecairEmpuxo(float dt)
    {
        empuxoAtual = Mathf.Lerp(empuxoAtual, 0f, dt / tempoRespostaMotor);
    }

    public void Desligar()
    {
        empuxoAtual = 0f;
    }
}