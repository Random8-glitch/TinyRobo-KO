using UnityEngine;
using UnityEngine.UI;

public class BarraDeVidaJugador : MonoBehaviour
{
    public enum Mode { FilledImage, ResizeRect }

    [Header("Opciones")]
    [SerializeField] private Mode modo = Mode.FilledImage;

    [Header("Referencias")]
    [SerializeField] private PlayerStats playerStats;
    [Tooltip("Usar con Mode = FilledImage")]
    [SerializeField] private Image fillImage; // Image con Type = Filled
    [Tooltip("Usar con Mode = ResizeRect. Debe tener Image si quieres color.")]
    [SerializeField] private RectTransform fillRect;

    [Header("Comportamiento")]
    [SerializeField] private float suavizado = 10f;

    private float currentFill = 1f;
    private Vector2 initialSizeDelta;
    private Image fillRectImage;

    private void Start()
    {
        if (playerStats == null)
        {
            playerStats = FindObjectOfType<PlayerStats>();
            if (playerStats == null)
            {
                Debug.LogWarning("BarraDeVida: no se encontró PlayerStats en la escena. Asigna la referencia en el inspector.");
            }
        }

        if (modo == Mode.FilledImage)
        {
            if (fillImage == null)
                Debug.LogError("BarraDeVida: asigna una Image (tipo Filled) en el inspector para el modo FilledImage.");
        }
        else // ResizeRect
        {
            if (fillRect == null)
                Debug.LogError("BarraDeVida: asigna un RectTransform para el modo ResizeRect.");
            else
            {
                initialSizeDelta = fillRect.sizeDelta;
                fillRectImage = fillRect.GetComponent<Image>();
            }
        }

        if (playerStats != null)
        {
            currentFill = Mathf.Clamp01(playerStats.vida / playerStats.vidaMax);

            if (modo == Mode.FilledImage && fillImage != null)
            {
                fillImage.fillAmount = currentFill;
                fillImage.color = Color.Lerp(Color.red, Color.green, currentFill);
            }
            else if (modo == Mode.ResizeRect && fillRect != null)
            {
                fillRect.sizeDelta = new Vector2(initialSizeDelta.x * currentFill, initialSizeDelta.y);
                if (fillRectImage != null)
                    fillRectImage.color = Color.Lerp(Color.red, Color.green, currentFill);
            }
        }
    }

    private void Update()
    {
        if (playerStats == null)
            return;

        float targetFill = Mathf.Clamp01(playerStats.vida / playerStats.vidaMax);
        currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * suavizado);

        if (modo == Mode.FilledImage)
        {
            if (fillImage == null) return;
            fillImage.fillAmount = currentFill;
            fillImage.color = Color.Lerp(Color.red, Color.green, currentFill);
        }
        else // ResizeRect
        {
            if (fillRect == null) return;
            fillRect.sizeDelta = new Vector2(initialSizeDelta.x * currentFill, initialSizeDelta.y);
            if (fillRectImage != null)
                fillRectImage.color = Color.Lerp(Color.red, Color.green, currentFill);
        }
    }

}
