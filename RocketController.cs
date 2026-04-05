using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(LineRenderer))]
public class RocketController : MonoBehaviour
{
    [Header("Motor")]
    public float potenciaMaxima = 200f;
    [Range(0f, 100f)] public float throttlePercentual = 100f;
    public float tempoRespostaMotor = 1.5f;

    private float empuxoAtual = 0f;

    [Header("Combustível")]
    public float combustivelMax = 20f;
    public float consumoPorSegundo = 2f;
    public float massaCombustivel = 20f;
    private float combustivelAtual;

    [Header("Massa estrutural")]
    public float massaEstrutural = 10f;

    [Header("Gravidade")]
    public float gravidadeEscala = 1f;

    [Header("Gimbal")]
    public float anguloMaxGimbal = 10f;
    public float velocidadeGimbal = 50f;
    private float anguloGimbalAtual = 0f;

    [Header("Alinhamento automático")]
    public float velocidadeAlinhamento = 5f;

    [Header("Atmosfera")]
    public float densidadeArNivelMar = 1.225f;
    public float alturaEscalaAtmosfera = 8000f;

    [Header("Arrasto")]
    public float coeficienteArrasto = 0.75f;
    public float areaFrontal = 0.1f;

    [Header("Estágio")]
    public float massaEstagio = 5f;
    public bool estagioSeparado = false;

    [Header("Trajetória")]
    public int passos = 60;
    public float tempoEntrePontos = 0.1f;

    [Header("Estado")]
    public bool lancando = false;

    private Rigidbody2D rb;
    private LineRenderer line;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        line = GetComponent<LineRenderer>();

        combustivelAtual = combustivelMax;

        AtualizarMassa();
        rb.gravityScale = gravidadeEscala;

        // Linha
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = Color.white;
        line.endColor = Color.white;

        // 🔥 Linha fina
        line.startWidth = 0.05f;
        line.endWidth = 0.05f;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
            anguloGimbalAtual += velocidadeGimbal * Time.deltaTime;

        if (Input.GetKey(KeyCode.RightArrow))
            anguloGimbalAtual -= velocidadeGimbal * Time.deltaTime;

        anguloGimbalAtual = Mathf.Clamp(anguloGimbalAtual, -anguloMaxGimbal, anguloMaxGimbal);

        if (Input.GetKeyDown(KeyCode.Space))
            lancando = true;

        if (Input.GetKeyDown(KeyCode.X) && !estagioSeparado)
        {
            massaEstrutural -= massaEstagio;
            estagioSeparado = true;
        }

        AlinharComForca();
        DesenharTrajetoria();
    }

    void FixedUpdate()
    {
        if (lancando && combustivelAtual > 0f)
            AplicarEmpuxo();

        AplicarArrasto();
        AtualizarMassa();
    }

    void AplicarEmpuxo()
    {
        float throttle = throttlePercentual / 100f;
        float empuxoAlvo = potenciaMaxima * throttle;

        empuxoAtual = Mathf.Lerp(empuxoAtual, empuxoAlvo, Time.fixedDeltaTime / tempoRespostaMotor);

        Vector2 direcao = Quaternion.Euler(0, 0, anguloGimbalAtual) * transform.up;

        rb.AddForce(direcao * empuxoAtual);

        float consumo = consumoPorSegundo * throttle * Time.fixedDeltaTime;
        combustivelAtual -= consumo;

        float massaConsumida = (consumo / combustivelMax) * massaCombustivel;
        massaCombustivel -= massaConsumida;

        combustivelAtual = Mathf.Clamp(combustivelAtual, 0f, combustivelMax);
        massaCombustivel = Mathf.Max(0f, massaCombustivel);
    }

    void AplicarArrasto()
    {
        Vector2 velocidade = rb.linearVelocity;
        float v = velocidade.magnitude;

        if (v < 0.01f) return;

        float altitude = transform.position.y;
        float densidadeAr = densidadeArNivelMar * Mathf.Exp(-altitude / alturaEscalaAtmosfera);

        float forcaArrasto = 0.5f * densidadeAr * v * v * coeficienteArrasto * areaFrontal;

        Vector2 direcao = -velocidade.normalized;

        rb.AddForce(direcao * forcaArrasto);
    }

    void AtualizarMassa()
    {
        rb.mass = massaEstrutural + massaCombustivel;
    }

    void AlinharComForca()
    {
        Vector2 velocidade = rb.linearVelocity;

        if (velocidade.magnitude < 0.1f) return;

        float anguloAlvo = Mathf.Atan2(velocidade.y, velocidade.x) * Mathf.Rad2Deg - 90f;

        float anguloAtual = transform.eulerAngles.z;

        float novoAngulo = Mathf.LerpAngle(anguloAtual, anguloAlvo, Time.deltaTime * velocidadeAlinhamento);

        transform.rotation = Quaternion.Euler(0, 0, novoAngulo);
    }

    void DesenharTrajetoria()
    {
        line.positionCount = passos;

        Vector2 pos = rb.position;
        Vector2 vel = rb.linearVelocity;

        float massaSimulada = rb.mass;
        float combustivelSimulado = combustivelAtual;
        float massaCombSimulada = massaCombustivel;

        for (int i = 0; i < passos; i++)
        {
            float altitude = pos.y;
            float densidadeAr = densidadeArNivelMar * Mathf.Exp(-altitude / alturaEscalaAtmosfera);

            Vector2 grav = Physics2D.gravity * rb.gravityScale;
            Vector2 accTotal = grav;

            // 🔥 Empuxo simulado
            if (combustivelSimulado > 0f)
            {
                float throttle = throttlePercentual / 100f;
                float empuxo = potenciaMaxima * throttle;

                Vector2 direcao = Quaternion.Euler(0, 0, anguloGimbalAtual) * transform.up;

                accTotal += direcao * (empuxo / massaSimulada);

                float consumo = consumoPorSegundo * throttle * tempoEntrePontos;
                combustivelSimulado -= consumo;

                float massaConsumida = (consumo / combustivelMax) * massaCombSimulada;
                massaCombSimulada -= massaConsumida;

                combustivelSimulado = Mathf.Max(0f, combustivelSimulado);
                massaCombSimulada = Mathf.Max(0f, massaCombSimulada);
            }

            // Atualiza massa simulada
            massaSimulada = massaEstrutural + massaCombSimulada;

            // 🌬️ Arrasto
            float v = vel.magnitude;
            if (v > 0.01f)
            {
                float arrasto = 0.5f * densidadeAr * v * v * coeficienteArrasto * areaFrontal;
                accTotal += -vel.normalized * (arrasto / massaSimulada);
            }

            vel += accTotal * tempoEntrePontos;
            pos += vel * tempoEntrePontos;

            line.SetPosition(i, pos);
        }
    }
}