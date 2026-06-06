using UnityEngine;

public class RocketGimbal : MonoBehaviour
{
    [Header("Gimbal")]
    [Tooltip("Deflexão máxima do bocal em graus")]
    public float anguloMaxGimbal  = 8f;
    [Tooltip("Graus por segundo ao pressionar a tecla")]
    public float velocidadeGimbal = 15f;

    [Header("Alinhamento Visual do Sprite")]
    [Tooltip("Velocidade de suavização da rotação visual")]
    public float velocidadeAlinhamento    = 1.5f;
    [Tooltip("Velocidade mínima (m/s) para iniciar alinhamento")]
    public float velocidadeMinAlinhamento = 2f;

    [HideInInspector] public float anguloGimbalAtual = 0f;
    [HideInInspector] public float rotacaoFisica     = 0f;
    [HideInInspector] public float rotacaoVisual     = 0f;

    /// <summary>Lê input do teclado e atualiza ângulo do gimbal.</summary>
    public void ProcessarInputGimbal(float dt)
    {
        if (Input.GetKey(KeyCode.LeftArrow))
            anguloGimbalAtual += velocidadeGimbal * dt;
        if (Input.GetKey(KeyCode.RightArrow))
            anguloGimbalAtual -= velocidadeGimbal * dt;

        anguloGimbalAtual = Mathf.Clamp(anguloGimbalAtual, -anguloMaxGimbal, anguloMaxGimbal);
    }

    /// <summary>
    /// Atualiza a rotação física do foguete baseado no torque do gimbal.
    /// Chame só quando o motor está ligado.
    /// </summary>
    public void AtualizarRotacaoFisica(float dt, float empuxoAtual, float potenciaMaxima)
    {
        if (potenciaMaxima <= 0f) return;

        // Torque proporcional ao gimbal e ao nível de empuxo relativo
        float torqueGimbal = -anguloGimbalAtual * 0.5f * (empuxoAtual / potenciaMaxima);
        rotacaoFisica += torqueGimbal * dt;

        // Amortecimento natural
        rotacaoFisica = Mathf.Lerp(rotacaoFisica, 0f, dt * 0.3f);
    }

    /// <summary>
    /// Alinha o sprite visualmente com a direção da velocidade.
    /// </summary>
    public void AlinharSpriteComVelocidade(Vector2 velocidade, float escala)
    {
        if (velocidade.magnitude * escala < velocidadeMinAlinhamento) return;

        float anguloAlvo  = Mathf.Atan2(velocidade.y, velocidade.x) * Mathf.Rad2Deg - 90f;
        rotacaoVisual     = Mathf.LerpAngle(rotacaoVisual, anguloAlvo, Time.deltaTime * velocidadeAlinhamento);
        transform.rotation = Quaternion.Euler(0f, 0f, rotacaoVisual);
    }

    public void Resetar()
    {
        anguloGimbalAtual = 0f;
        rotacaoFisica     = 0f;
    }
}