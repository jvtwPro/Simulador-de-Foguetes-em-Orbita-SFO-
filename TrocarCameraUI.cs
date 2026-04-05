using UnityEngine;

public class TrocarCameraUI : MonoBehaviour
{
    public Camera cameraFoguete;
    public Camera cameraMacro;

    public void AtivarCameraFoguete()
    {
        cameraFoguete.enabled = true;
        cameraMacro.enabled = false;
    }

    public void AtivarCameraMacro()
    {
        cameraFoguete.enabled = false;
        cameraMacro.enabled = true;
    }
}