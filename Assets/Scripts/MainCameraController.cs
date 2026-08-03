using UnityEngine;

/// Controlador de cámara top-down que mantiene el ángulo inicial pero permite
/// desplazar la cámara lateral/adelante/atrás para mantener al jugador dentro de la vista.
/// Dibuja la dead zone en Scene View y puede mostrar logs de depuración para inspeccionar comportamientos.
public class MainCameraController : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Transform del jugador o del objeto a mantener en cámara.")]
    [SerializeField] private Transform target;

    [Header("Dead zone (en unidades del mundo)")]
    [Tooltip("Si el objetivo sale de este margen en X/Z, la cámara se desplazará para mantenerlo dentro.")]
    [SerializeField] private Vector2 deadZone = new Vector2(2f, 2f);

    [Header("Movimiento")]
    [Tooltip("Velocidad de seguimiento (suavizado).")]
    [SerializeField] private float followSmoothTime = 0.15f;
    [Tooltip("Opcional: limitar la posición de la cámara dentro de estos límites (xmin, xmax, zmin, zmax).")]
    [SerializeField] private Vector4 bounds = new Vector4(-50f, 50f, -50f, 50f);
    [Tooltip("Activar para aplicar los límites.")]
    [SerializeField] private bool useBounds = false;

    [Header("Debug")]
    [SerializeField] private bool showDeadZone = true;
    [SerializeField] private Color deadZoneColor = new Color(1f, 0f, 0f, 0.15f);
    [Tooltip("Mostrar logs de delta/shift para depuración en runtime.")]
    [SerializeField] private bool debugLogs = false;

    // Conservamos la rotación y la altura iniciales para mantener el ángulo top-down
    private Quaternion initialRotation;
    private float initialHeight;
    private Vector3 velocity = Vector3.zero;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("MainCameraController: target no asignado. La cámara no seguirá a nadie.");
            return;
        }

        // Guardar rotación/altura para no modificarlas
        initialRotation = transform.rotation;
        initialHeight = transform.position.y;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        // Mantener siempre la misma rotación (ángulo top-down)
        transform.rotation = initialRotation;

        // Comparar en el plano XZ respecto a la posición actual de la cámara
        Vector3 camPos = transform.position;
        Vector2 camXZ = new Vector2(camPos.x, camPos.z);
        Vector2 targetXZ = new Vector2(target.position.x, target.position.z);

        Vector2 delta2D = targetXZ - camXZ; // delta.x => X, delta.y => Z

        Vector3 desiredPos = transform.position;

        // Ajustar X si sale de la dead zone (simétrico para ambas direcciones)
        if (Mathf.Abs(delta2D.x) > deadZone.x)
        {
            float shiftX = delta2D.x - Mathf.Sign(delta2D.x) * deadZone.x;
            desiredPos.x += shiftX;
            if (debugLogs) Debug.Log($"[Camera] shiftX={shiftX:F3} deltaX={delta2D.x:F3} deadZone.x={deadZone.x}");
        }

        // Ajustar Z si sale de la dead zone
        if (Mathf.Abs(delta2D.y) > deadZone.y)
        {
            float shiftZ = delta2D.y - Mathf.Sign(delta2D.y) * deadZone.y;
            desiredPos.z += shiftZ;
            if (debugLogs) Debug.Log($"[Camera] shiftZ={shiftZ:F3} deltaZ={delta2D.y:F3} deadZone.y={deadZone.y}");
        }

        // Mantener la altura original
        desiredPos.y = initialHeight;

        // Aplicar límites si están activados
        if (useBounds)
        {
            float clampedX = Mathf.Clamp(desiredPos.x, bounds.x, bounds.y); // xmin, xmax
            float clampedZ = Mathf.Clamp(desiredPos.z, bounds.z, bounds.w); // zmin, zmax

            if (debugLogs && (clampedX != desiredPos.x || clampedZ != desiredPos.z))
            {
                Debug.Log($"[Camera] desiredPos antes clamp: {desiredPos}, después clamp: ({clampedX}, {clampedZ})");
            }

            desiredPos.x = clampedX;
            desiredPos.z = clampedZ;
        }

        // Mover suavemente la cámara hasta la posición deseada
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, followSmoothTime);
    }

    // Dibuja la dead zone en la Scene View para facilitar ajuste visual
    private void OnDrawGizmos()
    {
        if (!showDeadZone)
            return;

        // Centro para dibujar: usar la posición actual de la cámara en XZ y la Y de la cámara
        Vector3 center = new Vector3(transform.position.x, transform.position.y, transform.position.z);

        // Tamaño de la caja en X,Z (doble del deadZone) y pequeña altura para que se vea en Scene View
        Vector3 size = new Vector3(deadZone.x * 2f, 0.1f, deadZone.y * 2f);

        Color fill = deadZoneColor;
        Color outline = new Color(deadZoneColor.r, deadZoneColor.g, deadZoneColor.b, Mathf.Clamp(deadZoneColor.a * 2f, 0.25f, 1f));

        Gizmos.color = fill;
        Gizmos.DrawCube(center, size);

        Gizmos.color = outline;
        Gizmos.DrawWireCube(center, size);
    }
}
