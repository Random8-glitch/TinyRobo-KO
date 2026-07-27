using UnityEngine;
using UnityEngine.UI;

public class BarraDeVidaEnemigo : MonoBehaviour
{
    public enum Mode { FilledImage, ResizeRect }

    [Header("Opciones")]
    [SerializeField] private Mode modo = Mode.FilledImage;

    [Header("Referencias")]
    [SerializeField] private EnemyStats enemyStats;
    [Tooltip("Usar con Mode = FilledImage")]
    [SerializeField] private Image fillImage; // Image con Type = Filled
    [Tooltip("Usar con Mode = ResizeRect. Debe ser el RectTransform del fill.")]
    [SerializeField] private RectTransform fillRect;

    [Header("Comportamiento")]
    [SerializeField] private float suavizado = 10f;

    private float currentFill = 1f;
    private Vector2 initialSizeDelta;
    private Image fillRectImage;

    private void Start()
    {
        // Intentar obtener EnemyStats automáticamente si no está asignado
        if (enemyStats == null)
        {
            enemyStats = GetComponentInParent<EnemyStats>();
            if (enemyStats == null)
            {
                Debug.LogWarning("BarraDeVidaEnemigo: no se encontró EnemyStats en el padre. Asigna la referencia en el inspector o llama SetEnemyStats.");
            }
        }

        if (modo == Mode.FilledImage)
        {
            if (fillImage == null)
                Debug.LogError("BarraDeVidaEnemigo: asigna una Image (tipo Filled) en el inspector para el modo FilledImage.");
        }
        else // ResizeRect
        {
            if (fillRect == null)
                Debug.LogError("BarraDeVidaEnemigo: asigna un RectTransform para el modo ResizeRect.");
            else
            {
                initialSizeDelta = fillRect.sizeDelta;
                fillRectImage = fillRect.GetComponent<Image>();
            }
        }

        if (enemyStats != null)
        {
            currentFill = Mathf.Clamp01(enemyStats.vida / enemyStats.vidaMax);
            ApplyFillInstant(currentFill);
        }
    }

    private void Update()
    {
        if (enemyStats == null)
            return;

        float targetFill = Mathf.Clamp01(enemyStats.vida / enemyStats.vidaMax);
        currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * suavizado);
        ApplyFillInstant(currentFill);
    }

    private void ApplyFillInstant(float fill)
    {
        if (modo == Mode.FilledImage)
        {
            if (fillImage == null) return;
            fillImage.fillAmount = fill;
            fillImage.color = Color.Lerp(Color.red, Color.green, fill);
        }
        else
        {
            if (fillRect == null) return;
            fillRect.sizeDelta = new Vector2(initialSizeDelta.x * fill, initialSizeDelta.y);
            if (fillRectImage != null)
                fillRectImage.color = Color.Lerp(Color.red, Color.green, fill);
        }
    }

    // Permite asignar la referencia desde código (útil para spawns)
    public void SetEnemyStats(EnemyStats stats)
    {
        enemyStats = stats;
        if (enemyStats != null)
        {
            currentFill = Mathf.Clamp01(enemyStats.vida / enemyStats.vidaMax);
            ApplyFillInstant(currentFill);
        }
    }
}
