
using UnityEngine;

public class MovimientoJugador : MonoBehaviour
{
    public float velocidad = 5f;

    void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 movimiento = new Vector3(horizontal, 0f, vertical).normalized;

        transform.position += movimiento * velocidad * Time.deltaTime;
    }
}