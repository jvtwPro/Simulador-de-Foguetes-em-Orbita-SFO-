using UnityEngine;

public class TrajetoriaFoguete : MonoBehaviour
{
    public LineRenderer lineRenderer;

    public int quantidadePontos = 100;
    public float tempoEntrePontos = 0.1f;

    public Vector3 velocidadeInicial;
    public Vector3 gravidade = new Vector3(0, -9.81f, 0);

    public void MostrarTrajetoria()
    {
        lineRenderer.positionCount = quantidadePontos;

        Vector3 posicaoInicial = transform.position;

        for (int i = 0; i < quantidadePontos; i++)
        {
            float tempo = i * tempoEntrePontos;

            Vector3 posicao = posicaoInicial 
                + velocidadeInicial * tempo 
                + 0.5f * gravidade * tempo * tempo;

            lineRenderer.SetPosition(i, posicao);
        }
    }
}