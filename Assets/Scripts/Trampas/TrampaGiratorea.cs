
using UnityEngine;

/// <summary>
/// Trampa giratoria con movimiento vertical y horizontal opcional.
/// Permite configurar límites, velocidades y pausas independientes.
/// </summary>
public class TrampaGiratorea : MonoBehaviour
{
    [Header("Referencia")]
    [Tooltip("Objeto que rotará y se moverá. Si está vacío, se usa este Transform.")]
    [SerializeField] private Transform objetoARotar;

    [Header("Rotación")]
    [SerializeField] private Vector3 ejeLocal = Vector3.up;
    [SerializeField] private float velocidadRotacion = 90f;
    [SerializeField] private bool usarEspacioLocal = true;
    [SerializeField] private bool rotarAlAwake = true;

    [Header("Movimiento vertical")]
    [SerializeField] private bool enableVerticalMovement = false;
    [SerializeField] private float minHeightOffset = -0.5f;
    [SerializeField] private float maxHeightOffset = 0.5f;
    [SerializeField] private float verticalSpeed = 1f;
    [SerializeField] private float verticalPauseAtExtremes = 0.5f;
    [SerializeField] private bool startMovingUp = true;

    [Header("Movimiento horizontal")]
    [Tooltip("Activa el desplazamiento sobre el eje X local.")]
    [SerializeField] private bool enableHorizontalMovement = false;

    [Tooltip("Distancia mínima respecto a la posición inicial en X.")]
    [SerializeField] private float minHorizontalOffset = -2f;

    [Tooltip("Distancia máxima respecto a la posición inicial en X.")]
    [SerializeField] private float maxHorizontalOffset = 2f;

    [Tooltip("Velocidad del movimiento horizontal.")]
    [SerializeField] private float horizontalSpeed = 1f;

    [Tooltip("Tiempo de pausa horizontal en cada extremo.")]
    [SerializeField] private float horizontalPauseAtExtremes = 0.5f;

    [Tooltip("Si está activo, comienza moviéndose hacia el extremo máximo.")]
    [SerializeField] private bool startMovingRight = true;

    private bool rotando;

    // Estado vertical
    private bool movingUp;
    private bool verticalPaused;
    private float verticalPauseTimer;
    private float baseLocalY;

    // Estado horizontal
    private bool movingRight;
    private bool horizontalPaused;
    private float horizontalPauseTimer;
    private float baseLocalX;

    private void Awake()
    {
        if (objetoARotar == null)
            objetoARotar = transform;

        rotando = rotarAlAwake;

        // Guardar la posición inicial local.
        baseLocalX = objetoARotar.localPosition.x;
        baseLocalY = objetoARotar.localPosition.y;

        // Corregir límites invertidos.
        if (minHeightOffset > maxHeightOffset)
        {
            (minHeightOffset, maxHeightOffset) =
                (maxHeightOffset, minHeightOffset);
        }

        if (minHorizontalOffset > maxHorizontalOffset)
        {
            (minHorizontalOffset, maxHorizontalOffset) =
                (maxHorizontalOffset, minHorizontalOffset);
        }

        movingUp = startMovingUp;
        movingRight = startMovingRight;

        verticalPaused = false;
        horizontalPaused = false;

        verticalPauseTimer = 0f;
        horizontalPauseTimer = 0f;
    }

    private void Update()
    {
        if (objetoARotar == null)
            return;

        // Rotación continua.
        if (rotando)
        {
            float step = velocidadRotacion * Time.deltaTime;

            if (usarEspacioLocal)
                objetoARotar.Rotate(ejeLocal.normalized, step, Space.Self);
            else
                objetoARotar.Rotate(ejeLocal.normalized, step, Space.World);
        }

        // Movimiento vertical.
        if (enableVerticalMovement)
            HandleVerticalMovement();

        // Movimiento horizontal independiente.
        if (enableHorizontalMovement)
            HandleHorizontalMovement();
    }

    private void HandleVerticalMovement()
    {
        if (verticalPaused)
        {
            verticalPauseTimer -= Time.deltaTime;

            if (verticalPauseTimer <= 0f)
                verticalPaused = false;

            return;
        }

        float targetY = baseLocalY +
            (movingUp ? maxHeightOffset : minHeightOffset);

        Vector3 lp = objetoARotar.localPosition;

        lp.y = Mathf.MoveTowards(
            lp.y,
            targetY,
            Mathf.Max(0f, verticalSpeed) * Time.deltaTime
        );

        objetoARotar.localPosition = lp;

        if (Mathf.Abs(lp.y - targetY) <= 0.001f)
        {
            verticalPaused = true;
            verticalPauseTimer = Mathf.Max(0f, verticalPauseAtExtremes);
            movingUp = !movingUp;
        }
    }

    private void HandleHorizontalMovement()
    {
        if (horizontalPaused)
        {
            horizontalPauseTimer -= Time.deltaTime;

            if (horizontalPauseTimer <= 0f)
                horizontalPaused = false;

            return;
        }

        float targetX = baseLocalX +
            (movingRight ? maxHorizontalOffset : minHorizontalOffset);

        Vector3 lp = objetoARotar.localPosition;

        lp.x = Mathf.MoveTowards(
            lp.x,
            targetX,
            Mathf.Max(0f, horizontalSpeed) * Time.deltaTime
        );

        objetoARotar.localPosition = lp;

        if (Mathf.Abs(lp.x - targetX) <= 0.001f)
        {
            horizontalPaused = true;
            horizontalPauseTimer =
                Mathf.Max(0f, horizontalPauseAtExtremes);

            movingRight = !movingRight;
        }
    }

    // API pública de rotación.

    public void StartRotation() => rotando = true;
    public void StopRotation() => rotando = false;

    public void SetSpeed(float nuevaVelocidad)
    {
        velocidadRotacion = nuevaVelocidad;
    }

    public float GetSpeed() => velocidadRotacion;

    public void RotateByDegrees(float grados)
    {
        if (objetoARotar == null) return;

        if (usarEspacioLocal)
            objetoARotar.Rotate(ejeLocal.normalized, grados, Space.Self);
        else
            objetoARotar.Rotate(ejeLocal.normalized, grados, Space.World);
    }

    public void SetTarget(Transform target)
    {
        objetoARotar = target != null ? target : transform;

        baseLocalX = objetoARotar.localPosition.x;
        baseLocalY = objetoARotar.localPosition.y;
    }

    // API pública de movimiento vertical.

    public void EnableVerticalMovement(bool enable)
    {
        enableVerticalMovement = enable;

        if (objetoARotar != null)
            baseLocalY = objetoARotar.localPosition.y;

        verticalPaused = false;
        verticalPauseTimer = 0f;
    }

    public void SetVerticalSpeed(float v)
    {
        verticalSpeed = Mathf.Max(0f, v);
    }

    public void SetVerticalOffsets(float minOffset, float maxOffset)
    {
        if (minOffset > maxOffset)
            (minOffset, maxOffset) = (maxOffset, minOffset);

        minHeightOffset = minOffset;
        maxHeightOffset = maxOffset;
    }

    public void SetVerticalPause(float seconds)
    {
        verticalPauseAtExtremes = Mathf.Max(0f, seconds);
    }

    // API pública de movimiento horizontal.

    public void EnableHorizontalMovement(bool enable)
    {
        enableHorizontalMovement = enable;

        if (objetoARotar != null)
            baseLocalX = objetoARotar.localPosition.x;

        horizontalPaused = false;
        horizontalPauseTimer = 0f;
    }

    public void SetHorizontalSpeed(float v)
    {
        horizontalSpeed = Mathf.Max(0f, v);
    }

    public void SetHorizontalOffsets(float minOffset, float maxOffset)
    {
        if (minOffset > maxOffset)
            (minOffset, maxOffset) = (maxOffset, minOffset);

        minHorizontalOffset = minOffset;
        maxHorizontalOffset = maxOffset;
    }

    public void SetHorizontalPause(float seconds)
    {
        horizontalPauseAtExtremes = Mathf.Max(0f, seconds);
    }
}
