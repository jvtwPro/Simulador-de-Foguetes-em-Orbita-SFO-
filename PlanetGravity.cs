using UnityEngine;

public class PlanetGravity : MonoBehaviour
{
    public Transform planeta;
    public float gravidade = 50f;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        AplicarGravidade();
        AlinharComPlaneta();
    }

    void AplicarGravidade()
    {
        Vector2 direcao = (planeta.position - transform.position).normalized;

        float distancia = Vector2.Distance(transform.position, planeta.position);

        // Gravidade estilo real (inversa ao quadrado da distância)
        float forca = gravidade / (distancia * distancia);

        rb.AddForce(direcao * forca);
    }

    void AlinharComPlaneta()
    {
        Vector2 direcao = (transform.position - planeta.position).normalized;

        float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0, 0, angulo - 90f);
    }
}