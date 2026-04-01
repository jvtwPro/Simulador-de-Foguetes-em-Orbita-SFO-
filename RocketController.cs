using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(LineRenderer))]
public class RocketController : MonoBehaviour
{
    [Header("Potência")]
    [Range(0f, 100f)]
    public float potenciaPercentual = 100f;
    public float forcaMaxima = 20f;

    [Header("Combustível")]
    public float combustivelMax = 10f;
    public float consumoPorSegundo = 1f;
    private float combustivelAtual;

    [Header("Massa")]
    public float massa = 1f;

    [Header("Controle")]
    public float controleRotacao = 50f;

    [Header("Trajetória")]
    public int passos = 50;
    public float tempoEntrePontos = 0.1f;

    [Header("Estado")]
    public bool lancando = false;

    private Rigidbody2D rb;
    private LineRenderer line;

    private float velocidadeAtual = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        line = GetComponent<LineRenderer>();

        rb.mass = massa;
        combustivelAtual = combustivelMax;
    }

    void Update()
    {
        // Ajuste de potência
        if (Input.GetKey(KeyCode.UpArrow))
            potenciaPercentual += 20f * Time.deltaTime;

        if (Input.GetKey(KeyCode.DownArrow))
            potenciaPercentual -= 20f * Time.deltaTime;

        potenciaPercentual = Mathf.Clamp(potenciaPercentual, 0f, 100f);

        // Controle de rotação (empuxo vetorial)
        if (Input.GetKey(KeyCode.LeftArrow))
            transform.Rotate(Vector3.forward * controleRotacao * Time.deltaTime);

        if (Input.GetKey(KeyCode.RightArrow))
            transform.Rotate(Vector3.back * controleRotacao * Time.deltaTime);

        // Iniciar lançamento
        if (Input.GetKeyDown(KeyCode.Space) && !lancando)
        {
            lancando = true;
        }

        // Desenhar trajetória
        DesenharTrajetoria();
    }

    void FixedUpdate()
    {
        if (lancando && combustivelAtual > 0f)
        {
            AplicarEmpuxo();
        }
    }

    void AplicarEmpuxo()
    {
        float potencia = potenciaPercentual / 100f;
        float forcaAtual = forcaMaxima * potencia;

        // Direção baseada na rotação (empuxo vetorial real)
        Vector2 direcao = transform.up;

        rb.AddForce(direcao * forcaAtual);

        // Consome combustível
        combustivelAtual -= consumoPorSegundo * Time.fixedDeltaTime;
        combustivelAtual = Mathf.Clamp(combustivelAtual, 0f, combustivelMax);

        // Física (debug)
        float aceleracao = forcaAtual / rb.mass;
        velocidadeAtual += aceleracao * Time.fixedDeltaTime;

        Debug.Log("Velocidade: " + velocidadeAtual.ToString("F2"));
        Debug.Log("Combustível: " + combustivelAtual.ToString("F2"));
    }

    void DesenharTrajetoria()
    {
        line.positionCount = passos;

        Vector2 posicao = rb.position;
        Vector2 velocidade = rb.linearVelocity;

        float potencia = potenciaPercentual / 100f;
        float forca = forcaMaxima * potencia;

        Vector2 aceleracaoEmpuxo = (transform.up * forca) / rb.mass;
        Vector2 gravidade = Physics2D.gravity * rb.gravityScale;

        for (int i = 0; i < passos; i++)
        {
            float t = i * tempoEntrePontos;

            // Se ainda tem combustível, considera empuxo
            Vector2 aceleracaoTotal = gravidade;

            if (combustivelAtual > 0)
                aceleracaoTotal += aceleracaoEmpuxo;

            Vector2 pos = posicao 
                        + velocidade * t 
                        + 0.5f * aceleracaoTotal * t * t;

            line.SetPosition(i, pos);

            // Detectar chão
            RaycastHit2D hit = Physics2D.Raycast(pos, Vector2.down, 0.1f);
            if (hit.collider != null)
            {
                line.positionCount = i + 1;
                break;
            }
        }
    }
}