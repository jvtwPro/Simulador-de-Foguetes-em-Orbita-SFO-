using UnityEngine;

public class CameraPan2D : MonoBehaviour
{
    public Transform alvo; // foguete

    [Header("Follow")]
    public float suavidade = 5f;

    [Header("Zoom (visão distante)")]
    public float zoomInicial = 30f;

    [Header("Movimento manual")]
    public float velocidadeArraste = 0.5f;

    private Camera cam;
    private Vector3 ultimaPosicaoMouse;

    void Start()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;

        cam.orthographicSize = zoomInicial;
    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            ArrastarCamera();
        }
        else
        {
            SeguirFoguete();
        }
    }

    void SeguirFoguete()
    {
        if (alvo == null) return;

        Vector3 posDesejada = new Vector3(
            alvo.position.x,
            alvo.position.y,
            transform.position.z
        );

        transform.position = Vector3.Lerp(
            transform.position,
            posDesejada,
            suavidade * Time.deltaTime
        );
    }

    void ArrastarCamera()
    {
        if (Input.GetMouseButtonDown(0))
        {
            ultimaPosicaoMouse = Input.mousePosition;
        }

        Vector3 diferenca = Input.mousePosition - ultimaPosicaoMouse;

        Vector3 movimento = new Vector3(-diferenca.x, -diferenca.y, 0);

        transform.Translate(movimento * velocidadeArraste * Time.deltaTime);

        ultimaPosicaoMouse = Input.mousePosition;
    }
}