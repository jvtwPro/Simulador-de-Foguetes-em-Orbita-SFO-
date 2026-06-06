using UnityEngine;

public class RocketCollision : MonoBehaviour
{
    public const float ESCALA = 100f;

    [Header("Colisão")]
    [Tooltip("Velocidade máxima de impacto para pouso suave (m/s)")]
    public float velocidadeImpactoMaximo = 5f;

    [HideInInspector] public bool colidiu = false;
    [HideInInspector] public PlanetaController planetaColisao;

    /// <summary>
    /// Invocado ao colidir. bool = pousou suave (true) ou destruído (false).
    /// </summary>
    public System.Action<bool> OnColisao;

    /// <summary>
    /// Verifica colisão por segment-vs-circle entre posAtual e novaPosicao.
    /// </summary>
    public bool Verificar(Vector2 posAtual, Vector2 novaPosicao, Vector2 novaVel, PlanetaController[] planetas)
    {
        foreach (PlanetaController p in planetas)
        {
            Vector2 centro   = (Vector2)p.transform.position;
            float raioUnity  = p.raio / ESCALA;

            // Ponto mais próximo no segmento de movimento
            Vector2 segmento = novaPosicao - posAtual;
            float lenSq      = segmento.sqrMagnitude;
            float t          = (lenSq > 0f)
                ? Mathf.Clamp01(Vector2.Dot(centro - posAtual, segmento) / lenSq)
                : 0f;

            Vector2 pontoProximo = posAtual + t * segmento;
            float distMin        = Vector2.Distance(pontoProximo, centro);

            if (distMin > raioUnity) continue;

            // Confirmação: nova posição dentro do raio
            if (Vector2.Distance(novaPosicao, centro) <= raioUnity)
            {
                planetaColisao = p;
                colidiu        = true;
                ProcessarColisao(novaPosicao, novaVel, p, raioUnity);
                return true;
            }
        }
        return false;
    }

    void ProcessarColisao(Vector2 novaPosicao, Vector2 novaVel, PlanetaController p, float raioUnity)
    {
        float velImpacto  = novaVel.magnitude * ESCALA; // m/s
        bool  pousouSuave = velImpacto <= velocidadeImpactoMaximo;

        if (pousouSuave)
        {
            // Reposiciona na superfície do planeta
            Vector2 normal     = (novaPosicao - (Vector2)p.transform.position).normalized;
            transform.position = (Vector2)p.transform.position + normal * (raioUnity + 0.01f);
            Debug.Log($"✅ Pouso suave em {p.name}! Velocidade de impacto: {velImpacto:F1} m/s");
        }
        else
        {
            Debug.Log($"💥 Foguete destruído em {p.name}! Velocidade de impacto: {velImpacto:F1} m/s");
        }

        OnColisao?.Invoke(pousouSuave);
    }
}