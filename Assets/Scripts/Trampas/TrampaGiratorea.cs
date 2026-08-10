using UnityEngine;

/// <summary>
/// Trampa giratoria con desplazamiento vertical opcional.
/// Rota el `objetoARotar` y (si está activado) lo mueve arriba/abajo entre
/// `minHeightOffset` y `maxHeightOffset`, pausando en los extremos.
/// </summary>
public class TrampaGiratorea : MonoBehaviour
{
    [Header("Referencia")]
    [Tooltip("Objeto que será rotado. Si está vacío, se usará el Transform propio.")]
    [SerializeField] private Transform objetoARotar;

    [Header("Rotación")]
    [Tooltip("Eje local alrededor del que rotará el objeto (en unidades de Vector3).")]
    [SerializeField] private Vector3 ejeLocal = Vector3.up;
    [Tooltip("Velocidad de rotación en grados por segundo.")]
    [SerializeField] private float velocidadRotacion = 90f;
    [Tooltip("Si es true usa el espacio local del objeto a rotar; si es false usa espacio mundial.")]
    [SerializeField] private bool usarEspacioLocal = true;
    [Tooltip("Permite activar/desactivar la rotación desde el Inspector.")]
    [SerializeField] private bool rotarAlAwake = true;

    [Header("Movimiento vertical")]
    [Tooltip("Habilita movimiento vertical (arriba/abajo).")]
    [SerializeField] private bool enableVerticalMovement = false;
    [Tooltip("Desplazamiento mínimo relativo a la posición local inicial (negativo = abajo).")]
    [SerializeField] private float minHeightOffset = -0.5f;
    [Tooltip("Desplazamiento máximo relativo a la posición local inicial (positivo = arriba).")]
    [SerializeField] private float maxHeightOffset = 0.5f;
    [Tooltip("Velocidad de movimiento vertical en unidades por segundo.")]
    [SerializeField] private float verticalSpeed = 1f;
    [Tooltip("Tiempo (s) que se queda detenido en cada extremo.")]
    [SerializeField] private float pauseAtExtremes = 0.5f;
    [Tooltip("Inicio del movimiento vertical en dirección hacia arriba si true, si no hacia abajo.")]
    [SerializeField] private bool startMovingUp = true;

    private bool rotando;
    private bool movingUp;
    private bool paused;
    private float pauseTimer;
    private float baseLocalY;

    private void Awake()
    {
        if (objetoARotar == null)
            objetoARotar = transform;

        rotando = rotarAlAwake;

        // Guardar Y local base para desplazamientos relativos
        baseLocalY = objetoARotar.localPosition.y;

        // Asegurar offsets coherentes
        if (minHeightOffset > maxHeightOffset)
        {
            float tmp = minHeightOffset;
            minHeightOffset = maxHeightOffset;
            maxHeightOffset = tmp;
        }

        movingUp = startMovingUp;
        paused = false;
        pauseTimer = 0f;
    }

    private void Update()
    {
        // Rotación
        if (rotando && objetoARotar != null)
        {
            float step = velocidadRotacion * Time.deltaTime;
            if (usarEspacioLocal)
                objetoARotar.Rotate(ejeLocal.normalized, step, Space.Self);
            else
                objetoARotar.Rotate(ejeLocal.normalized, step, Space.World);
        }

        // Movimiento vertical
        if (enableVerticalMovement && objetoARotar != null)
        {
            HandleVerticalMovement();
        }
    }

    private void HandleVerticalMovement()
    {
        if (paused)
        {
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f)
            {
                paused = false;
            }
            return;
        }

        float targetLocalY = baseLocalY + (movingUp ? maxHeightOffset : minHeightOffset);
        Vector3 lp = objetoARotar.localPosition;
        lp.y = Mathf.MoveTowards(lp.y, targetLocalY, verticalSpeed * Time.deltaTime);
        objetoARotar.localPosition = lp;

        if (Mathf.Abs(lp.y - targetLocalY) <= 0.001f)
        {
            // Llegó al extremo: pausar y cambiar dirección
            paused = true;
            pauseTimer = Mathf.Max(0f, pauseAtExtremes);
            movingUp = !movingUp;
        }
    }

    // API pública

    public void StartRotation() => rotando = true;
    public void StopRotation() => rotando = false;
    public void SetSpeed(float nuevaVelocidad) => velocidadRotacion = nuevaVelocidad;
    public float GetSpeed() => velocidadRotacion;
    public void SetTarget(Transform target)
    {
        objetoARotar = target ?? transform;
        baseLocalY = objetoARotar.localPosition.y;
    }

    // Vertical control API
    public void EnableVerticalMovement(bool enable)
    {
        enableVerticalMovement = enable;
        // recalcular base para suavizar cambio
        if (objetoARotar != null) baseLocalY = objetoARotar.localPosition.y;
        paused = false;
    }

    public void SetVerticalSpeed(float v) => verticalSpeed = Mathf.Max(0f, v);
    public void SetVerticalOffsets(float minOffset, float maxOffset)
    {
        if (minOffset > maxOffset) (minOffset, maxOffset) = (maxOffset, minOffset);
        minHeightOffset = minOffset;
        maxHeightOffset = maxOffset;
    }

    public void SetPauseAtExtremes(float seconds) => pauseAtExtremes = Mathf.Max(0f, seconds);

    public void RotateByDegrees(float grados)
    {
        if (objetoARotar == null) return;
        if (usarEspacioLocal)
            objetoARotar.Rotate(ejeLocal.normalized, grados, Space.Self);
        else
            objetoARotar.Rotate(ejeLocal.normalized, grados, Space.World);
    }
}
