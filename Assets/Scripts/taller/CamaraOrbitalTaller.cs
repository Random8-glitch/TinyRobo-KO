
using UnityEngine;
using UnityEngine.InputSystem;

public class CamaraOrbitalTaller : MonoBehaviour
{
    [Header("Punto de enfoque")]
    [SerializeField] private Transform centroCamara;

    [Header("Rotacion")]
    [SerializeField] private float sensibilidadX = 0.3f;
    [SerializeField] private float sensibilidadY = 0.3f;
    [SerializeField] private float limiteVerticalMin = -20f;
    [SerializeField] private float limiteVerticalMax = 80f;

    [Header("Distancia")]
    [SerializeField] private float distancia = 5f;
    [SerializeField] private float distanciaMinima = 2f;
    [SerializeField] private float distanciaMaxima = 10f;
    [SerializeField] private float velocidadZoom = 1f;

    private float rotacionX;
    private float rotacionY;

    private void Start()
    {
        if (centroCamara == null)
        {
            Debug.LogError("No se ha asignado el Centro de Camara.");
            enabled = false;
            return;
        }

        // Obtener la rotacion inicial de la camara
        Vector3 direccion = transform.position - centroCamara.position;

        rotacionX = Mathf.Atan2(direccion.x, direccion.z) * Mathf.Rad2Deg;

        rotacionY = Mathf.Asin(
            direccion.y / direccion.magnitude
        ) * Mathf.Rad2Deg;

        ActualizarCamara();
    }

    private void LateUpdate()
    {
        if (Mouse.current == null)
            return;

        // Rotacion manteniendo presionado el boton derecho
        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 movimiento = Mouse.current.delta.ReadValue();

            rotacionX += movimiento.x * sensibilidadX;
            rotacionY -= movimiento.y * sensibilidadY;

            rotacionY = Mathf.Clamp(
                rotacionY,
                limiteVerticalMin,
                limiteVerticalMax
            );
        }

        // Zoom con la rueda del mouse
        float scroll = Mouse.current.scroll.ReadValue().y;

        distancia -= scroll * velocidadZoom * 0.01f;

        distancia = Mathf.Clamp(
            distancia,
            distanciaMinima,
            distanciaMaxima
        );

        ActualizarCamara();
    }

    private void ActualizarCamara()
    {
        Quaternion rotacion = Quaternion.Euler(
            rotacionY,
            rotacionX,
            0f
        );

        Vector3 desplazamiento = rotacion * new Vector3(
            0f,
            0f,
            -distancia
        );

        transform.position = centroCamara.position + desplazamiento;

        transform.LookAt(centroCamara.position);
    }
}
