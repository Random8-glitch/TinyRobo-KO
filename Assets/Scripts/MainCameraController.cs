
using UnityEngine;

/// Controlador de cámara top-down con búsqueda automática
/// del jugador mediante el Layer "Player".
 [DefaultExecutionOrder(200)]
public class MainCameraController : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Se asigna automáticamente buscando el Layer Player.")]
    [SerializeField] private Transform target;

    [Header("Dead zone (en unidades del mundo)")]
    [SerializeField] private Vector2 deadZone = new Vector2(2f, 2f);

    [Header("Movimiento")]
    [SerializeField] private float followSmoothTime = 0.15f;

    [SerializeField]
    private Vector4 bounds =
        new Vector4(-50f, 50f, -50f, 50f);

    [SerializeField] private bool useBounds = false;

    [Header("Debug")]
    [SerializeField] private bool showDeadZone = true;
    [SerializeField]
    private Color deadZoneColor =
        new Color(1f, 0f, 0f, 0.15f);

    [SerializeField] private bool debugLogs = false;

    private Quaternion initialRotation;
    private float initialHeight;
    private Vector3 velocity = Vector3.zero;

    private void Start()
    {
        // Guardar rotación y altura inicial
        initialRotation = transform.rotation;
        initialHeight = transform.position.y;

        // Buscar automáticamente al jugador
        BuscarJugador();
    }

    private void BuscarJugador()
    {
        int playerLayer = LayerMask.NameToLayer("Player");

        if (playerLayer == -1)
        {
            Debug.LogWarning(
                "MainCameraController: No existe el Layer Player."
            );
            return;
        }

        // Buscar objetos activos en la escena
        GameObject[] objetos = FindObjectsByType<GameObject>(
            FindObjectsSortMode.None
        );

        foreach (GameObject obj in objetos)
        {
            if (obj.layer == playerLayer)
            {
                target = obj.transform;

                if (debugLogs)
                {
                    Debug.Log(
                        $"[Camera] Jugador encontrado: {obj.name}"
                    );
                }

                return;
            }
        }

        Debug.LogWarning(
            "MainCameraController: No se encontró ningún objeto con Layer Player."
        );
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        // Mantener rotación original
        transform.rotation = initialRotation;

        Vector3 camPos = transform.position;

        Vector2 camXZ = new Vector2(camPos.x, camPos.z);

        Vector2 targetXZ = new Vector2(
            target.position.x,
            target.position.z
        );

        Vector2 delta2D = targetXZ - camXZ;

        Vector3 desiredPos = transform.position;

        // Movimiento horizontal X
        if (Mathf.Abs(delta2D.x) > deadZone.x)
        {
            float shiftX =
                delta2D.x -
                Mathf.Sign(delta2D.x) * deadZone.x;

            desiredPos.x += shiftX;

            if (debugLogs)
            {
                Debug.Log(
                    $"[Camera] shiftX={shiftX:F3} " +
                    $"deltaX={delta2D.x:F3}"
                );
            }
        }

        // Movimiento horizontal Z
        if (Mathf.Abs(delta2D.y) > deadZone.y)
        {
            float shiftZ =
                delta2D.y -
                Mathf.Sign(delta2D.y) * deadZone.y;

            desiredPos.z += shiftZ;

            if (debugLogs)
            {
                Debug.Log(
                    $"[Camera] shiftZ={shiftZ:F3} " +
                    $"deltaZ={delta2D.y:F3}"
                );
            }
        }

        // Mantener altura original
        desiredPos.y = initialHeight;

        // Aplicar límites
        if (useBounds)
        {
            float clampedX = Mathf.Clamp(
                desiredPos.x,
                bounds.x,
                bounds.y
            );

            float clampedZ = Mathf.Clamp(
                desiredPos.z,
                bounds.z,
                bounds.w
            );

            if (debugLogs &&
                (clampedX != desiredPos.x ||
                 clampedZ != desiredPos.z))
            {
                Debug.Log(
                    $"[Camera] Posición limitada: " +
                    $"({clampedX}, {clampedZ})"
                );
            }

            desiredPos.x = clampedX;
            desiredPos.z = clampedZ;
        }

        // Movimiento suavizado
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPos,
            ref velocity,
            followSmoothTime
        );
    }

    private void OnDrawGizmos()
    {
        if (!showDeadZone)
            return;

        Vector3 center = new Vector3(
            transform.position.x,
            transform.position.y,
            transform.position.z
        );

        Vector3 size = new Vector3(
            deadZone.x * 2f,
            0.1f,
            deadZone.y * 2f
        );

        Color fill = deadZoneColor;

        Color outline = new Color(
            deadZoneColor.r,
            deadZoneColor.g,
            deadZoneColor.b,
            Mathf.Clamp(deadZoneColor.a * 2f, 0.25f, 1f)
        );

        Gizmos.color = fill;
        Gizmos.DrawCube(center, size);

        Gizmos.color = outline;
        Gizmos.DrawWireCube(center, size);
    }
}
