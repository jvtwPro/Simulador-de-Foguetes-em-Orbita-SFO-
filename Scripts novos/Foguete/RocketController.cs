using UnityEngine;

[RequireComponent(typeof(RocketMotor))]
[RequireComponent(typeof(RocketGimbal))]
[RequireComponent(typeof(RocketPhysics))]
[RequireComponent(typeof(RocketCollision))]
[RequireComponent(typeof(RocketTrajectory))]
public class RocketController : MonoBehaviour
{
    public const float ESCALA = 100f;

    [Header("Estado")]
    public bool lancando = false;

    [Header("Telemetria (SI) — somente leitura")]
    public float velocidadeAtual;      // m/s
    public float aceleracaoAtual;      // m/s²
    public float altitudeAtual;        // m
    public float empuxoAtualN;         // N
    public float massaAtualKg;         // kg
    public float combustivelRestanteKg;// kg

    private RocketMotor      motor;
    private RocketGimbal     gimbal;
    private RocketPhysics    fisica;
    private RocketCollision  colisao;
    private RocketTrajectory trajetoria;

    // Tempo desde o início do voo — usado para ignorar colisão no instante do lançamento
    private float tempoVoo = 0f;

    void Awake()
    {
        motor      = GetComponent<RocketMotor>();
        gimbal     = GetComponent<RocketGimbal>();
        fisica     = GetComponent<RocketPhysics>();
        colisao    = GetComponent<RocketCollision>();
        trajetoria = GetComponent<RocketTrajectory>();

        fisica.planetas   = FindObjectsOfType<PlanetaController>();
        fisica.posicao    = transform.position;
        fisica.velocidade = Vector2.zero;

        if (fisica.planetas.Length == 0)
            Debug.LogWarning("[RocketController] Nenhum PlanetaController encontrado na cena!");

        colisao.OnColisao += (pousouSuave) =>
        {
            motor.Desligar();
            gimbal.Resetar();
            trajetoria.Limpar();
            lancando          = false;
            fisica.velocidade = Vector2.zero;
            fisica.posicao    = transform.position;
        };
    }

    void Update()
    {
        if (colisao.colidiu) return;

        // SPACE: inicia lançamento
        if (Input.GetKeyDown(KeyCode.Space))
            lancando = true;

        // Setas: controle de gimbal durante o voo
        if (lancando && motor.TemCombustivel)
            gimbal.ProcessarInputGimbal(Time.deltaTime);

        // X: separação de estágio
        if (Input.GetKeyDown(KeyCode.X))
            fisica.SepararEstagio();

        // Alinha sprite com a direção da velocidade
        gimbal.AlinharSpriteComVelocidade(fisica.velocidade, ESCALA);

        // Atualiza previsão de trajetória
        trajetoria.Desenhar(
            fisica.posicao,            fisica.velocidade,
            motor.massaCombustivel,    fisica.massaEstrutural,
            motor.empuxoAtual,         gimbal.rotacaoFisica,     gimbal.anguloGimbalAtual,
            lancando,                  motor.throttlePercentual,
            motor.potenciaMaxima,      motor.consumoPorSegundo,  motor.tempoRespostaMotor,
            fisica.coeficienteArrasto, fisica.areaFrontal,
            fisica.planetas);
    }

    void FixedUpdate()
    {
        if (colisao.colidiu) return;

        float dt        = Time.fixedDeltaTime;
        float massaAtual = fisica.MassaTotal(motor.massaCombustivel);

        // ── EMPUXO ────────────────────────────────────────────────────────
        Vector2 empuxoVec = Vector2.zero;
        if (lancando && motor.TemCombustivel)
        {
            empuxoVec = motor.CalcularEmpuxo(dt, gimbal.rotacaoFisica, gimbal.anguloGimbalAtual);
            gimbal.AtualizarRotacaoFisica(dt, motor.empuxoAtual, motor.potenciaMaxima);
        }
        else
        {
            motor.DecairEmpuxo(dt);
        }

        // Telemetria bruta
        empuxoAtualN           = motor.empuxoAtual;
        massaAtualKg           = massaAtual;
        combustivelRestanteKg  = motor.massaCombustivel;

        // ── ACELERAÇÕES ───────────────────────────────────────────────────

        // Velocidade em m/s para cálculo de arrasto
        Vector2 velMS = fisica.velocidade * ESCALA;

        // Gravidade + arrasto → unidades Unity/s²
        Vector2 accGravArrasto = fisica.CalcularGravArrasto(fisica.posicao, velMS, massaAtual);

        // Empuxo: N / kg = m/s²  →  / ESCALA = unidades Unity/s²
        Vector2 accEmpuxo = (massaAtual > 0f)
            ? (empuxoVec / massaAtual) / ESCALA
            : Vector2.zero;

        Vector2 accTotal = accGravArrasto + accEmpuxo;

        // ── TELEMETRIA SI ─────────────────────────────────────────────────
        velocidadeAtual = fisica.velocidade.magnitude * ESCALA;  // m/s
        aceleracaoAtual = accTotal.magnitude * ESCALA;            // m/s²
        altitudeAtual   = fisica.AltitudePlanetaMaisProximo(fisica.posicao); // m

        // ── INTEGRAÇÃO ────────────────────────────────────────────────────
        var (novaVel, novaPosicao) = fisica.Integrar(
            fisica.velocidade, fisica.posicao, accTotal, dt);

        // Conta tempo de voo apenas quando em movimento
        if (lancando || fisica.velocidade.sqrMagnitude > 0.0001f)
            tempoVoo += dt;

        // ── DETECÇÃO DE COLISÃO ───────────────────────────────────────────
        // Aguarda 0.5s para evitar colisão falsa no instante do lançamento
        if (tempoVoo > 0.5f)
        {
            if (colisao.Verificar(fisica.posicao, novaPosicao, novaVel, fisica.planetas))
                return; // colisao.OnColisao já foi disparado internamente
        }

        // ── ATUALIZA ESTADO ───────────────────────────────────────────────
        fisica.velocidade  = novaVel;
        fisica.posicao     = novaPosicao;
        transform.position = fisica.posicao;
    }
}