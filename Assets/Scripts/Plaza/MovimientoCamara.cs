
using UnityEngine;

public class MovimientoCamara : MonoBehaviour
{
    public float sensibilidad = 100f;

    private float rotacionX = 0f;

    void Start()
    {
        // Ocultar y bloquear el cursor en el centro de la pantalla
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensibilidad * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensibilidad * Time.deltaTime;

        // Control vertical de la cámara
        rotacionX -= mouseY;
        rotacionX = Mathf.Clamp(rotacionX, -90f, 90f);

        transform.localRotation = Quaternion.Euler(rotacionX, 0f, 0f);

        // Control horizontal del jugador
        transform.parent.Rotate(Vector3.up * mouseX);
    }
}
