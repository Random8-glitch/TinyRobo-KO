using UnityEngine;

public class LimiteCamara : MonoBehaviour
{
    [Header("Limites del movimiento de la camara")]
    [SerializeField] private float minimoX = -10f;
    [SerializeField] private float maximoX = 10f;

    [SerializeField] private float minimoZ = -10f;
    [SerializeField] private float maximoZ = 10f;

    private void LateUpdate()
    {
        Vector3 posicion = transform.position;

        // Limitar movimiento horizontal en X.
        posicion.x = Mathf.Clamp(
            posicion.x,
            minimoX,
            maximoX
        );

        // Limitar movimiento horizontal en Z.
        posicion.z = Mathf.Clamp(
            posicion.z,
            minimoZ,
            maximoZ
        );

        // Mantener la altura original de la camara.
        transform.position = posicion;
    }
}
