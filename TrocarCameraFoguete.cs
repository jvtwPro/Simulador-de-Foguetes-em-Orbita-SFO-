using UnityEngine;

public class TrocarCameraFoguete : MonoBehaviour
{
    public Camera cameraFoguete;   // câmera que segue o foguete
    public Camera cameraMacro;     // câmera distante

    public TrajetoriaFoguete trajetoriaScript;

    void Start()
    {
        // começa com a câmera do foguete ativa
        cameraFoguete.enabled = true;
        cameraMacro.enabled = false;
    }

    void OnMouseDown()
    {
        // troca para a câmera macro
        cameraFoguete.enabled = false;
        cameraMacro.enabled = true;

        // mostra a trajetória
        if (trajetoriaScript != null)
        {
            trajetoriaScript.MostrarTrajetoria();
        }
    }
}