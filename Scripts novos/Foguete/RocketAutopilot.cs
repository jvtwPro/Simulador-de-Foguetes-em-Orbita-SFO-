using UnityEngine;

/// <summary>
/// Piloto automático orbital.
/// Adicione no mesmo GameObject do RocketController.
///
/// FASES:
///   1. LANCAMENTO      — sobe verticalmente
///   2. GRAVITY_TURN    — inclina para horizontal conforme ganha altitude
///   3. APOGEU          — corta motor, sobe por inércia
///   4. CIRCULARIZACAO  — queima horizontal no apogeu
///   5. ORBITA          — mantém órbita
///
/// TECLA O — ativa/desativa
/// </summary>
[RequireComponent(typeof(RocketController))]
[RequireComponent(typeof(RocketMotor))]
[RequireComponent(typeof(RocketGimbal))]
[RequireComponent(typeof(RocketPhysics))]
public class RocketAutopilot : MonoBehaviour
{
    // ── Parâmetros expostos ────────────────────────────────────────────────

    [Header("Piloto Automático")]
    public bool ativo = false;

    [Header("Parâmetros de Missão")]
    [Tooltip("Altitude alvo da órbita circular (metros)")]
    public float altitudeOrbitalAlvo = 200000f;

    [Tooltip("Altitude para iniciar a gravity turn (metros)")]
    public float altitudeInicioTurn  = 3000f;

    [Tooltip("Altitude para cortar o motor e subir em balística (metros)")]
    public float altitudeCorteMotor  = 160000f;

    [Tooltip("Margem de velocidade radial para detectar apogeu (m/s)")]
    public float margemApogeu = 2f;

    [Tooltip("Margem de velocidade para considerar órbita fechada (m/s)")]
    public float toleranciaOrbita = 15f;

    [Header("Telemetria (somente leitura)")]
    public string faseAtual          = "Aguardando";
    public float  velOrbitalAlvo_ms  = 0f;
    public float  velRadial_ms       = 0f;
    public float  velTangencial_ms   = 0f;
    public float  apogeu_m           = 0f;
    public float  perigeu_m          = 0f;
    public float  excentricidade     = 0f;

    // ── Privados ───────────────────────────────────────────────────────────

    private RocketController controlador;
    private RocketMotor      motor;
    private RocketGimbal     gimbal;
    private RocketPhysics    fisica;
    private PlanetaController planeta;

    private enum Fase { Aguardando, Lancamento, GravityTurn, Balistica, Circularizacao, Orbita }
    private Fase fase = Fase.Aguardando;

    // Direção de órbita: +1 = anti-horário
    private const float SENTIDO = 1f;

    // ── Unity ──────────────────────────────────────────────────────────────

    void Awake()
    {
        controlador = GetComponent<RocketController>();
        motor       = GetComponent<RocketMotor>();
        gimbal      = GetComponent<RocketGimbal>();
        fisica      = GetComponent<RocketPhysics>();
    }

    void Start()
    {
        AtualizarPlaneta();
        if (planeta != null)
            velOrbitalAlvo_ms = VelOrbital(planeta.raio + altitudeOrbitalAlvo);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            ativo = !ativo;
            if (!ativo)
            {
                fase = Fase.Aguardando;
                motor.throttlePercentual = 0f;
            }
            Debug.Log($"[Autopilot] {(ativo ? "ATIVADO" : "DESATIVADO")}");
        }
        faseAtual = fase.ToString();
    }

    void FixedUpdate()
    {
        if (!ativo) return;
        AtualizarPlaneta();
        if (planeta == null) return;
        AtualizarTelemetria();
        ExecutarFase();
    }

    // ── Máquina de estados ─────────────────────────────────────────────────

    void ExecutarFase()
    {
        float alt = fisica.AltitudePlanetaMaisProximo(fisica.posicao);

        switch (fase)
        {
            // ── 1. Aguardando ─────────────────────────────────────────────
            case Fase.Aguardando:
                controlador.lancando     = true;
                motor.throttlePercentual = 100f;
                fase = Fase.Lancamento;
                Debug.Log("[Autopilot] FASE 1 — Lançamento vertical");
                break;

            // ── 2. Lançamento vertical ────────────────────────────────────
            case Fase.Lancamento:
                motor.throttlePercentual = 100f;
                // Mantém gimbal zerado — sobe reto
                gimbal.anguloGimbalAtual = 0f;
                gimbal.rotacaoFisica     = 0f;

                if (alt >= altitudeInicioTurn)
                {
                    fase = Fase.GravityTurn;
                    Debug.Log($"[Autopilot] FASE 2 — Gravity Turn @ {alt:F0} m");
                }
                break;

            // ── 3. Gravity Turn ───────────────────────────────────────────
            case Fase.GravityTurn:
                motor.throttlePercentual = 100f;

                float prog = Mathf.Clamp01(
                    (alt - altitudeInicioTurn) /
                    (altitudeCorteMotor - altitudeInicioTurn));

                // Direções relativas ao planeta
                Vector2 radial     = DirRadial();
                Vector2 tangencial = DirTangencial(radial);

                // Interpolação esférica: vertical → horizontal
                Vector2 dirAlvo = Vector2.Lerp(radial, tangencial, prog * prog).normalized;

                // Aplica diretamente a rotação física para apontar nessa direção
                // (mais confiável que usar o gimbal que depende do motor)
                AplicarRotacaoDireta(dirAlvo);

                if (alt >= altitudeCorteMotor)
                {
                    motor.throttlePercentual = 0f;
                    fase = Fase.Balistica;
                    Debug.Log($"[Autopilot] FASE 3 — Balística @ {alt:F0} m | vRad={velRadial_ms:F1} m/s");
                }
                break;

            // ── 4. Balística até o apogeu ─────────────────────────────────
            case Fase.Balistica:
                motor.throttlePercentual = 0f;

                // Aponta horizontalmente durante a subida (prepara circularização)
                Vector2 radB = DirRadial();
                AplicarRotacaoDireta(DirTangencial(radB));

                // Apogeu detectado: velocidade radial próxima de zero e caindo
                bool noApogeu      = velRadial_ms < margemApogeu;
                bool altOk         = alt >= altitudeOrbitalAlvo * 0.85f;

                if (noApogeu && altOk)
                {
                    motor.throttlePercentual = 100f;
                    fase = Fase.Circularizacao;
                    Debug.Log($"[Autopilot] FASE 4 — Circularização @ {alt:F0} m | vTan={velTangencial_ms:F1} m/s | alvo={VelOrbital(planeta.raio + alt):F1} m/s");
                }
                break;

            // ── 5. Circularização ─────────────────────────────────────────
            case Fase.Circularizacao:
                Vector2 radC = DirRadial();
                AplicarRotacaoDireta(DirTangencial(radC));

                float velAlvoC = VelOrbital(planeta.raio + alt);
                float delta    = velAlvoC - velTangencial_ms;

                if (delta > toleranciaOrbita)
                {
                    // Throttle proporcional — evita overshooting
                    motor.throttlePercentual = Mathf.Clamp(delta / 30f * 100f, 15f, 100f);
                }
                else
                {
                    motor.throttlePercentual = 0f;
                    fase = Fase.Orbita;
                    Debug.Log($"[Autopilot] FASE 5 — ÓRBITA! alt={alt:F0} m | exc={excentricidade:F4}");
                }
                break;

            // ── 6. Órbita ─────────────────────────────────────────────────
            case Fase.Orbita:
                motor.throttlePercentual = 0f;

                // Correção se perigeu entrar na atmosfera
                if (perigeu_m < planeta.alturaEscalaAtmosfera * 3f)
                {
                    Vector2 radO = DirRadial();
                    AplicarRotacaoDireta(DirTangencial(radO));
                    motor.throttlePercentual = 25f;
                    Debug.Log($"[Autopilot] Correção — perigeu={perigeu_m:F0} m");
                }
                break;
        }
    }

    // ── Controle de atitude ────────────────────────────────────────────────

    /// <summary>
    /// Aponta o foguete diretamente na direção desejada manipulando rotacaoFisica.
    /// Muito mais confiável do que depender do gimbal sozinho.
    /// </summary>
    void AplicarRotacaoDireta(Vector2 direcaoAlvo)
    {
        if (direcaoAlvo.sqrMagnitude < 0.001f) return;

        // Ângulo que o foguete deveria ter (sprite aponta para +Y = 90°)
        float anguloAlvo  = Mathf.Atan2(direcaoAlvo.y, direcaoAlvo.x) * Mathf.Rad2Deg - 90f;

        // Rotação visual atual
        float anguloAtual = gimbal.rotacaoVisual;

        float diff = Mathf.DeltaAngle(anguloAtual, anguloAlvo);

        // Aplica como rotação física suavizada
        float velocidadeRotacao = 45f; // graus/s
        float passo = Mathf.Clamp(diff, -velocidadeRotacao * Time.fixedDeltaTime,
                                          velocidadeRotacao * Time.fixedDeltaTime);

        gimbal.rotacaoFisica  += passo;
        gimbal.rotacaoVisual   = Mathf.LerpAngle(gimbal.rotacaoVisual, anguloAlvo,
                                                  Time.fixedDeltaTime * 3f);

        // Gimbal neutro — o controle é feito pela rotacaoFisica
        gimbal.anguloGimbalAtual = Mathf.Lerp(gimbal.anguloGimbalAtual, 0f,
                                               Time.fixedDeltaTime * 5f);
    }

    // ── Helpers de direção ─────────────────────────────────────────────────

    Vector2 DirRadial()
        => (fisica.posicao - (Vector2)planeta.transform.position).normalized;

    Vector2 DirTangencial(Vector2 radial)
        => new Vector2(-radial.y, radial.x) * SENTIDO;

    float VelOrbital(float raioM)
        => (planeta != null && raioM > 0f) ? Mathf.Sqrt(planeta.GM / raioM) : 0f;

    // ── Telemetria orbital ─────────────────────────────────────────────────

    void AtualizarTelemetria()
    {
        Vector2 posRel  = fisica.posicao - (Vector2)planeta.transform.position;
        Vector2 dirRad  = posRel.normalized;
        Vector2 velMS   = fisica.velocidade * RocketPhysics.ESCALA;

        velRadial_ms      = Vector2.Dot(velMS, dirRad);
        velTangencial_ms  = Vector2.Dot(velMS, new Vector2(-dirRad.y, dirRad.x));

        float r  = posRel.magnitude * RocketPhysics.ESCALA;
        float v  = velMS.magnitude;
        float GM = planeta.GM;

        velOrbitalAlvo_ms = VelOrbital(planeta.raio + altitudeOrbitalAlvo);

        if (GM > 0f && r > 0f && v > 0.1f)
        {
            float energia  = v * v * 0.5f - GM / r;
            float semiEixo = (energia < -1f) ? -GM / (2f * energia) : float.MaxValue;

            // Vetor excentricidade
            Vector2 eVec = ((v * v / GM) - (1f / r)) * posRel
                         - (Vector2.Dot(posRel, velMS) / GM) * velMS;
            excentricidade = eVec.magnitude;

            if (semiEixo < 1e9f && excentricidade < 1f)
            {
                apogeu_m  = semiEixo * (1f + excentricidade) - planeta.raio;
                perigeu_m = semiEixo * (1f - excentricidade) - planeta.raio;
            }
        }
    }

    void AtualizarPlaneta()
    {
        if (fisica?.planetas == null || fisica.planetas.Length == 0) return;
        PlanetaController prox = null;
        float menor = float.MaxValue;
        foreach (var p in fisica.planetas)
        {
            float d = Vector2.Distance(fisica.posicao, (Vector2)p.transform.position);
            if (d < menor) { menor = d; prox = p; }
        }
        planeta = prox;
    }

    // ── Gizmos ────────────────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        if (!ativo || planeta == null || fisica == null) return;

        // Órbita alvo — círculo verde
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.4f);
        float rOrb = (planeta.raio + altitudeOrbitalAlvo) / RocketPhysics.ESCALA;
        DesenharCirculo((Vector2)planeta.transform.position, rOrb);

        // Vetor velocidade — amarelo
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(fisica.posicao, fisica.velocidade.normalized * 100f);

        // Vetor radial — ciano
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(fisica.posicao, DirRadial() * 80f);
    }

    void DesenharCirculo(Vector2 centro, float raio)
    {
        int n = 72;
        Vector3 ant = centro + new Vector2(raio, 0f);
        for (int i = 1; i <= n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            Vector3 prox = (Vector3)(centro + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * raio);
            Gizmos.DrawLine(ant, prox);
            ant = prox;
        }
    }
}