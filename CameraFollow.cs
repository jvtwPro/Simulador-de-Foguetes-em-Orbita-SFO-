using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform alvo; // foguete

    [Header("Suavização")]
    public float suavidade = 5f;

    [Header("Zoom")]
    public float zoom = 5f;
    public float zoomMin = 2f;
    public float zoomMax = 20f;
    public float velocidadeZoom = 5f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        SeguirAlvo();
        ControlarZoom();
    }

    void SeguirAlvo()
    {
        if (alvo == null) return;

        Vector3 posDesejada = new Vector3(alvo.position.x, alvo.position.y, transform.position.z);

        transform.position = Vector3.Lerp(transform.position, posDesejada, suavidade * Time.deltaTime);
    }

    void ControlarZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        zoom -= scroll * velocidadeZoom;
        zoom = Mathf.Clamp(zoom, zoomMin, zoomMax);

        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, zoom, Time.deltaTime * 5f);
    }
}