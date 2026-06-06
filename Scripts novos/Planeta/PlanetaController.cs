using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlanetaController : MonoBehaviour
{
    // 1 unidade Unity = ESCALA metros
    public const float ESCALA = 100f;

    [Header("Física")]
    public float raio                = 200000f; // metros
    public float gravidadeSuperficie = 9.81f;   // m/s²

    [Header("Atmosfera")]
    public float densidadeArNivelMar    = 1.225f; // kg/m³
    public float alturaEscalaAtmosfera  = 8500f;  // metros

    // GM = g * R² — único valor usado na física, calculado automaticamente
    // Exposto no Inspector apenas para leitura/debug
    [Header("Calculado automaticamente (não editar)")]
    public float GM;   // m³/s²
    public float massa; // kg (só display)

    void OnValidate()
    {
        Recalcular();
    }

    void Awake()
    {
        Recalcular();

        Rigidbody2D rb  = GetComponent<Rigidbody2D>();
        rb.bodyType     = RigidbodyType2D.Static;
        rb.gravityScale = 0f;

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        col.radius    = raio / ESCALA;
        col.isTrigger = true;
    }

    void Start()
    {
        float gVerif = GM / (raio * raio);
        Debug.Log($"[{gameObject.name}] raio={raio} m | GM={GM:E3} | g verificado={gVerif:F4} m/s² | raio Unity={raio/ESCALA:F1} u");

        if (Mathf.Abs(gVerif - gravidadeSuperficie) > 0.01f)
            Debug.LogError($"[{gameObject.name}] GM inconsistente! Esperado {gravidadeSuperficie}, obtido {gVerif:F4}");
    }

    void Recalcular()
    {
        if (raio <= 0f) return;

        // GM = g × R²  (m³/s²)
        // Esta é a única fórmula que importa para a física.
        // Não usamos G real (6.674e-11) pois float não tem precisão suficiente.
        GM = gravidadeSuperficie * raio * raio;

        // Massa em kg para exibição — usa double internamente para evitar overflow
        double GMd = (double)gravidadeSuperficie * (double)raio * (double)raio;
        double G_real = 6.674e-11;
        massa = (float)(GMd / G_real);
    }
}