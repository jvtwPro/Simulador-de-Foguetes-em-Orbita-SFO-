using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryLine : MonoBehaviour
{
    public int passos = 50;
    public float tempoEntrePontos = 0.1f;

    public Rigidbody2D rb;
    public RocketController rocket;

    private LineRenderer line;

    void Start()
    {
        line = GetComponent<LineRenderer>();
    }

    void Update()
    {
        DesenharTrajetoria();
    }

    void DesenharTrajetoria()
    {
        line.positionCount = passos;

        Vector2 posicao = rb.position;
        Vector2 velocidade = rb.linearVelocity;

        float potencia = rocket.potenciaPercentual / 100f;
        float forca = rocket.forcaMaxima * potencia;

        Vector2 aceleracao = (transform.up * forca) / rb.mass;
        Vector2 gravidade = Physics2D.gravity * rb.gravityScale;

        for (int i = 0; i < passos; i++)
        {
            float t = i * tempoEntrePontos;

            Vector2 pos = posicao 
                        + velocidade * t 
                        + 0.5f * (aceleracao + gravidade) * t * t;

            line.SetPosition(i, pos);

            // Parar no "solo"
            RaycastHit2D hit = Physics2D.Raycast(pos, Vector2.down, 0.1f);
            if (hit.collider != null)
            {
                line.positionCount = i + 1;
                break;
            }
        }
    }
}