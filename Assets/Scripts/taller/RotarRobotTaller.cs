
using UnityEngine;
using UnityEngine.InputSystem;

public class RotarRobotTaller : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform puntoTaller;
    [SerializeField] private Transform modeloRobot;

    [Header("Rotacion")]
    [SerializeField] private float sensibilidad = 0.3f;

    private void Update()
    {
        if (modeloRobot == null || puntoTaller == null)
            return;

        if (Mouse.current == null)
            return;

        // Rotar mientras se mantiene presionado
        // el boton izquierdo del mouse
        if (Mouse.current.leftButton.isPressed)
        {
            float movimientoX =
                Mouse.current.delta.ReadValue().x;

            float rotacion = movimientoX * sensibilidad;

            modeloRobot.RotateAround(
                puntoTaller.position,
                Vector3.up,
                rotacion
            );
        }
    }

    // Permite asignar un robot nuevo
    public void AsignarRobot(Transform nuevoRobot)
    {
        modeloRobot = nuevoRobot;
    }
}
